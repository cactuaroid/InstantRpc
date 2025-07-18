﻿using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace InstantRpc.Test.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel();

            // expose this instance to InstantRpc with Dispatcher.Invoke() wrapper so that it can access UI parts
            InstantRpcService.Expose(this, (a) => Dispatcher.Invoke(a), (f) => Dispatcher.Invoke(f));
        }
    }
}