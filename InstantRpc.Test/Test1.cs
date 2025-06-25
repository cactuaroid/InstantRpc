using InstantRpc.Test.Wpf;
using System.Diagnostics;

namespace InstantRpc.Test
{
    [TestClass]
    public sealed class Test1
    {
        private static Process _app;
        private static InstantRpcClient<MainWindow> _client;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            _app = Process.Start("InstantRpc.Test.Wpf.exe");
            _client = new InstantRpcClient<MainWindow>();
            _client.WaitUntilExposed(TimeSpan.FromSeconds(5));
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _app.Kill();
        }

        [TestMethod]
        public void TestMethod1()
        {
            var top = _client.Get((x) => x.Top);

            Assert.AreNotEqual(0, top);
        }
    }
}
