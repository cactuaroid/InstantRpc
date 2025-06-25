using InstantRpc.Test.Wpf;
using System.Diagnostics;
using System.Windows;

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
        public void GetSet_Primitive()
        {
            _client.Set((x) => x.Top, 10);
            Assert.AreEqual(10, _client.Get((x) => x.Top));
        }

        [TestMethod]
        public void GetSet_Enum()
        {
            _client.Set((x) => x.Visibility, Visibility.Collapsed);
            Assert.AreEqual(Visibility.Collapsed, _client.Get((x) => x.Visibility));
            _client.Set((x) => x.Visibility, Visibility.Visible);
            Assert.AreEqual(Visibility.Visible, _client.Get((x) => x.Visibility));
        }

        [TestMethod]
        public void Invoke()
        {
            _client.Invoke((x) => x.Hide());
            Assert.IsFalse(_client.Get((x) => x.IsVisible));
            _client.Invoke((x) => x.Show());
            Assert.IsTrue(_client.Get((x) => x.IsVisible));
        }
    }
}
