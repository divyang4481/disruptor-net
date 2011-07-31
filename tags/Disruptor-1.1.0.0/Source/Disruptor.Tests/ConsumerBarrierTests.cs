using System;
using System.Threading;
using Disruptor.Tests.Support;
using Moq;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class ConsumerBarrierTests
    {
        private RingBuffer<StubData> _ringBuffer;
        private Mock<IBatchConsumer> _consumerMock1;
        private Mock<IBatchConsumer> _consumerMock2;
        private Mock<IBatchConsumer> _consumerMock3;
        private IConsumerBarrier<StubData> _consumerBarrier;

        [SetUp]
        public void SetUp()
        {
            _ringBuffer = new RingBuffer<StubData>(()=>new StubData(-1), 64);

            _consumerMock1 = new Mock<IBatchConsumer>();
            _consumerMock2 = new Mock<IBatchConsumer>();
            _consumerMock3 = new Mock<IBatchConsumer>();

            _consumerBarrier = _ringBuffer.CreateConsumerBarrier(_consumerMock1.Object, _consumerMock2.Object, _consumerMock3.Object);
            _ringBuffer.SetTrackedConsumer(new NoOpConsumer<StubData>(_ringBuffer));
        }

        [Test]
        public void ShouldWaitForWorkCompleteWhereCompleteWorkThresholdIsAhead() 
        {
            const int expectedNumberMessages = 10;
            const int expectedWorkSequence = 9;
            FillRingBuffer(expectedNumberMessages);

            _consumerMock1.SetupGet(c => c.Sequence).Returns(expectedNumberMessages);
            _consumerMock2.SetupGet(c => c.Sequence).Returns(expectedNumberMessages);
            _consumerMock3.SetupGet(c => c.Sequence).Returns(expectedNumberMessages);

            var waitForResult = _consumerBarrier.WaitFor(expectedWorkSequence);
            Assert.IsTrue(waitForResult.AvailableSequence >= expectedWorkSequence);

            _consumerMock1.Verify();
            _consumerMock2.Verify();
            _consumerMock3.Verify();
        }

        [Test]
        public void ShouldWaitForWorkCompleteWhereAllWorkersAreBlockedOnRingBuffer() 
        {
            const long expectedNumberMessages = 10;
            FillRingBuffer(expectedNumberMessages);

            var workers = new StubConsumer[3];
            for (var i = 0; i < workers.Length; i++)
            {
                workers[i] = new StubConsumer(expectedNumberMessages - 1);
            }

            var consumerBarrier = _ringBuffer.CreateConsumerBarrier(workers);

            new Thread(() =>
                    {
                        StubData data;
                        var sequence = _ringBuffer.NextEntry(out data);
                        data.Value = (int)sequence;
                        _ringBuffer.Commit(sequence);

                        foreach (var stubWorker in workers)
                        {
                            stubWorker.Sequence = sequence;
                        }
                    })
                    .Start();

            const long expectedWorkSequence = expectedNumberMessages;
            var waitForResult = consumerBarrier.WaitFor(expectedNumberMessages);
            Assert.IsTrue(waitForResult.AvailableSequence >= expectedWorkSequence);
        }

        [Test]
        public void ShouldInterruptDuringBusySpin()
        {
            const long expectedNumberMessages = 10;
            FillRingBuffer(expectedNumberMessages);

            var countdownEvent = new CountdownEvent(9);

            _consumerMock1.SetupGet(c => c.Sequence).Returns(8L).Callback(() => countdownEvent.Signal());
            _consumerMock2.SetupGet(c => c.Sequence).Returns(8L).Callback(() => countdownEvent.Signal());
            _consumerMock3.SetupGet(c => c.Sequence).Returns(8L).Callback(() =>
                                                                              {
                                                                                  countdownEvent.Signal();
                                                                                  Thread.Sleep(100); // wait a bit to prevent race (otherwise the WaitStrategy 
                                                                                  // does another iterations which decrease the countdown event below 0
                                                                              });

            var alerted = new[] { false };
            var t = new Thread(() =>
            {
            	if(_consumerBarrier.WaitFor(expectedNumberMessages - 1).IsAlerted)
            		alerted[0] = true;
            });

            t.Start();
            Assert.IsTrue(countdownEvent.Wait(TimeSpan.FromMilliseconds(100000000)));
            _consumerBarrier.Alert();
            t.Join();

            Assert.IsTrue(alerted[0], "Thread was not interrupted");

            _consumerMock1.Verify();
            _consumerMock2.Verify();
            _consumerMock3.Verify();
        }

        [Test]
        public void ShouldWaitForWorkCompleteWhereCompleteWorkThresholdIsBehind() 
        {
            const long expectedNumberMessages = 10;
            FillRingBuffer(expectedNumberMessages);

            var entryConsumers = new StubConsumer[3];
            for (var i = 0; i < entryConsumers.Length; i++)
            {
                entryConsumers[i] = new StubConsumer(expectedNumberMessages - 2);
            }

            var consumerBarrier = _ringBuffer.CreateConsumerBarrier(entryConsumers);

            new Thread(()=>
                           {
                                foreach (var stubWorker in entryConsumers)
                                {
                                    stubWorker.Sequence = stubWorker.Sequence + 1;
                                }
                           }).Start();

            const long expectedWorkSequence = expectedNumberMessages - 1;
            var waitForResult = consumerBarrier.WaitFor(expectedWorkSequence);
            Assert.IsTrue(waitForResult.AvailableSequence >= expectedWorkSequence);
        }

        [Test]
        public void ShouldSetAndClearAlertStatus()
        {
            Assert.IsFalse(_consumerBarrier.IsAlerted);

            _consumerBarrier.Alert();
            Assert.IsTrue(_consumerBarrier.IsAlerted);

            _consumerBarrier.ClearAlert();
            Assert.IsFalse(_consumerBarrier.IsAlerted);
        }

        private void FillRingBuffer(long expectedNumberMessages)
        {
            for (var i = 0; i < expectedNumberMessages; i++)
            {
                StubData data;
                var sequence = _ringBuffer.NextEntry(out data);
                data.Value = i;
                _ringBuffer.Commit(sequence);
            }
        }

        private class StubConsumer : IBatchConsumer
        {
            public StubConsumer(long sequence)
            {
                _sequence = sequence;
            }

            private long _sequence;

            public void Run()
            {
            }

            public void DelaySequenceWrite(int period)
            {
                
            }

            public long Sequence
            {
                get
                {
                    return Thread.VolatileRead(ref _sequence);
                }
                set
                {
                    Thread.VolatileWrite(ref _sequence, value);
                }
            }

            public bool Running
            {
                get { return true; }
            }

            public void Halt()
            {
            }
        }
    }
}