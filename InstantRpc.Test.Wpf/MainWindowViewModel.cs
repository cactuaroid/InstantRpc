using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstantRpc.Test.Wpf
{
    public class MainWindowViewModel
    {
        public string Value { get; set; } = "test";

        public string GetValue()
            => Value;

        public int Add(int a, int b)
            => a + b;

        public string Concat(MyParam a, MyParam b)
            => $"{a.Value}{b.Value}";
    }

    public class MyParam
    {
        public string Value { get; set; }
        
        public MyParam()
        {
        }

        public MyParam(string value1, string value2)
        {
            Value = (value1 ?? "") + (value2 ?? "");
        }
    }
}
