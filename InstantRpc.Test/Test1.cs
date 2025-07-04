using InstantRpc.Test.Wpf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            // Ensure no previous instance is running
            Process.GetProcessesByName("InstantRpc.Test.Wpf").FirstOrDefault()?.Kill();

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
            var v = _client.Get((x) => x.Top);
            Assert.AreEqual(10, v);
        }

        [TestMethod]
        public void GetSet_Enum()
        {
            _client.Set((x) => x.Visibility, Visibility.Collapsed);

            var v1 = _client.Get((x) => x.Visibility);
            Assert.AreEqual(Visibility.Collapsed, v1);

            _client.Set((x) => x.Visibility, Visibility.Visible);

            var v2 = _client.Get((x) => x.Visibility);
            Assert.AreEqual(Visibility.Visible, v2);
        }

        [TestMethod]
        public void Invoke()
        {
            _client.Invoke((x) => x.Hide());

            var v1 = _client.Get((x) => x.IsVisible);
            Assert.IsFalse(v1);

            _client.Invoke((x) => x.Show());

            var v2 = _client.Get((x) => x.IsVisible);
            Assert.IsTrue(v2);
        }

        [TestMethod]
        public void GetSetInvoke_PropertyChain()
        {
            _client.Set((x) => ((MainWindowViewModel)x.DataContext).Value, "changed");
            var v1 = _client.Get((x) => ((MainWindowViewModel)x.DataContext).Value);
            var v2 = _client.Invoke((x) => ((MainWindowViewModel)x.DataContext).GetValue());
            Assert.AreEqual("changed", v1);
            Assert.AreEqual("changed", v2);
        }

        [TestMethod]
        public void Invoke_PropertyChain_PrimitiveArgs()
        {
            var v = _client.Invoke((x) => ((MainWindowViewModel)x.DataContext).Add(1, 2));
            Assert.AreEqual(3, v);
        }

        [TestMethod]
        public void Invoke_PropertyChain_ConstructorArgs()
        {
            var v = _client.Invoke((x) => ((MainWindowViewModel)x.DataContext).Concat(new MyParam("1", "2"), new MyParam() { Value = "3" }));
            Assert.AreEqual("123", v);
        }

        [TestMethod]
        public void GetSetInvoke_Tuple()
        {
            _client.Set((x) => ((MainWindowViewModel)x.DataContext).Tuple, (3, 4));
            var v1 = _client.Get((x) => ((MainWindowViewModel)x.DataContext).Tuple);
            var v2 = _client.Invoke((x) => ((MainWindowViewModel)x.DataContext).GetTuple());
            Assert.AreEqual((3, 4), v1);
            Assert.AreEqual((3, 4), v2);
        }

        [TestMethod]
        public void GetSetInvoke_ParsableClass()
        {
            _client.Set((x) => ((MainWindowViewModel)x.DataContext).ParsableValue, new MyParam("1", "2"));
            var v1 = _client.Get((x) => ((MainWindowViewModel)x.DataContext).ParsableValue);
            var v2 = _client.Invoke((x) => ((MainWindowViewModel)x.DataContext).GetParsableValue());
            Assert.AreEqual("12", v1.Value);
            Assert.AreEqual("12", v2.Value);
        }
    }
}
