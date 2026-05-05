using TestFramework;
using TestSubject;

namespace Tests
{
    [TestClass]
    public class SharedContextTests
    {
        [SharedSetUp]
        public static void SharedSetUp()
        {
            DataStore.ResetShared();
        }

        [SharedTearDown]
        public static void SharedTearDown()
        {
            DataStore.ResetShared();
        }

        [TestMethod]
        public void Test1_IncrementShared()
        {
            DataStore.IncrementShared();
            Assert.AreEqual(1, DataStore.SharedCounter);
        }

        [TestMethod]
        public void Test2_IncrementSharedAgain()
        {
            DataStore.IncrementShared();
            Assert.AreEqual(2, DataStore.SharedCounter);
        }

        [TestMethod]
        public void Test3_CheckSharedValue()
        {
            Assert.AreEqual(2, DataStore.SharedCounter);
        }
    }
}