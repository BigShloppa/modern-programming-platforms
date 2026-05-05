using System.Threading.Tasks;
using TestFramework;
using TestSubject;

namespace Tests
{
    [TestClass]
    public class ParallelPerformanceTests
    {
        private Calculator _calc;

        [SetUp]
        public void SetUp() => _calc = new Calculator();

        [TestMethod]
        [Timeout(500)]
        public async Task FastAsyncTest()
        {
            var result = await _calc.SlowAddAsync(1, 2, 50);
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        [Timeout(300)]
        public async Task MediumAsyncTest()
        {
            var result = await _calc.SlowAddAsync(3, 4, 100);
            Assert.AreEqual(7, result);
        }

        [TestMethod]
        [Timeout(1000)]
        public async Task SlowAsyncTest()
        {
            var result = await _calc.SlowAddAsync(5, 6, 200);
            Assert.AreEqual(11, result);
        }

        [TestMethod]
        [Timeout(200)]
        public void SyncWaitTest()
        {
            var val = _calc.SleepSort(42, 80);
            Assert.AreEqual(42, val);
        }

        /*
        [TestMethod]
        [Timeout(50)]
        public async Task TimeoutFailTest()
        {
            await Task.Delay(200);
            Assert.IsTrue(false);
        }
        */
    }
}