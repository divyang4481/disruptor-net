using System;
using System.Threading;
using Disruptor.Tests.Support;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class LifecycleAwareTests
    {
        private readonly ManualResetEvent _startMru = new ManualResetEvent(false);
        private readonly ManualResetEvent _shutdownMru = new ManualResetEvent(false);
        private readonly RingBuffer<StubData> _ringBuffer = new RingBuffer<StubData>(()=>new StubData(-1), 16);
        private IDependencyBarrier<StubData> _dependencyBarrier;
        private LifecycleAwareEventHandler _eventHandler;
        private EventProcessor<StubData> _eventProcessor;

        [SetUp]
        public void SetUp()
        {
            _dependencyBarrier = _ringBuffer.CreateBarrier();
            _eventHandler = new LifecycleAwareEventHandler(_startMru, _shutdownMru);
            _eventProcessor = new EventProcessor<StubData>(_dependencyBarrier, _eventHandler);
        }

        [Test]
        public void ShouldNotifyOfEventProcessorLifecycle()
        {
            new Thread(_eventProcessor.Run).Start();

            _startMru.WaitOne();

            _eventProcessor.Halt();

            _shutdownMru.WaitOne();

            Assert.AreEqual(_eventHandler.StartCounter, 1);
            Assert.AreEqual(_eventHandler.ShutdownCounter, 1);
        }
        
        private sealed class LifecycleAwareEventHandler : IEventHandler<StubData>, ILifecycleAware
        {
            private readonly ManualResetEvent _startMru;
            private readonly ManualResetEvent _shutdownMru;
            private int _startCounter;
            private int _shutdownCounter;

            public int StartCounter
            {
                get { return _startCounter; }
            }

            public int ShutdownCounter
            {
                get { return _shutdownCounter; }
            }

            public LifecycleAwareEventHandler(ManualResetEvent startMru, ManualResetEvent shutdownMru)
            {
                _startMru = startMru;
                _shutdownMru = shutdownMru;
            }

            public void OnAvailable(long sequence, StubData data)
            {
            }

            public void OnEndOfBatch()
            {
            }

            public void OnStart()
            {
                ++_startCounter;
                _startMru.Set();
            }

            public void OnStop()
            {
                ++_shutdownCounter;
                _shutdownMru.Set();
            }
        }
    }    
}