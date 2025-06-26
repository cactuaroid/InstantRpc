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

        [TestMethod]
        public void GetSetInvoke_PropertyChain()
        {
            _client.Set((x) => ((MainWindowViewModel)x.DataContext).Value, "changed");
            Assert.AreEqual("changed", _client.Get((x) => ((MainWindowViewModel)x.DataContext).Value));
            Assert.AreEqual("changed", _client.Invoke((x) => ((MainWindowViewModel)x.DataContext).GetValue()));
        }

        [TestMethod]
        public void Invoke_PropertyChain_PrimitiveArgs()
        {
            Assert.AreEqual(3, _client.Invoke((x) => ((MainWindowViewModel)x.DataContext).Add(1, 2)));
        }

        [TestMethod]
        public void Invoke_PropertyChain_ConstructorArgs()
        {
            Assert.AreEqual("123", _client.Invoke((x) => ((MainWindowViewModel)x.DataContext).Concat(new MyParam("1", "2"), new MyParam() { Value = "3" })));
        }

        [TestMethod]
        public void GetSetInvoke_Tuple()
        {
            _client.Set((x) => ((MainWindowViewModel)x.DataContext).Tuple, (3, 4));
            Assert.AreEqual((3, 4), _client.Get((x) => ((MainWindowViewModel)x.DataContext).Tuple));
            Assert.AreEqual((3, 4), _client.Invoke((x) => ((MainWindowViewModel)x.DataContext).GetTuple()));
        }

        [TestMethod]
        public void GetSetInvoke_ParsableClass()
        {
            _client.Set((x) => ((MainWindowViewModel)x.DataContext).ParsableValue, new MyParam("1", "2"));
            Assert.AreEqual("12", _client.Get((x) => ((MainWindowViewModel)x.DataContext).ParsableValue).Value);
            Assert.AreEqual("12", _client.Invoke((x) => ((MainWindowViewModel)x.DataContext).GetParsableValue()).Value);
        }
    }
}
