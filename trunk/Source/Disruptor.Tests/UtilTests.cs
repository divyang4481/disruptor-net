using Moq;
using NUnit.Framework;

namespace Disruptor.Tests
{
    [TestFixture]
    public class UtilTests
    {
        [Test]
        public void ShouldReturnNextPowerOfTwo()
        {
            var powerOfTwo = Util.CeilingNextPowerOfTwo(1000);

            Assert.AreEqual(1024, powerOfTwo);
        }

        [Test]
        public void ShouldReturnExactPowerOfTwo()
        {
            var powerOfTwo = Util.CeilingNextPowerOfTwo(1024);

            Assert.AreEqual(1024, powerOfTwo);
        }

        [Test]
        public void ShouldReturnMinimumSequence()
        {
            var consumerMock1 = new Mock<IConsumer>();
            var consumerMock2 = new Mock<IConsumer>();
            var consumerMock3 = new Mock<IConsumer>();

            var consumers = new[] {consumerMock1.Object, consumerMock2.Object, consumerMock3.Object};

            consumerMock1.SetupGet(c => c.Sequence).Returns(11);
            consumerMock2.SetupGet(c => c.Sequence).Returns(4);
            consumerMock3.SetupGet(c => c.Sequence).Returns(13);

            Assert.AreEqual(4L, Util.GetMinimumSequence(consumers));

            consumerMock1.Verify();
            consumerMock2.Verify();
            consumerMock3.Verify();
        }

        [Test]
        public void ShouldReturnLongMaxWhenNoConsumers()
        {
            var consumers = new IConsumer[0];

            Assert.AreEqual(long.MaxValue, Util.GetMinimumSequence(consumers));
        }
    }
}