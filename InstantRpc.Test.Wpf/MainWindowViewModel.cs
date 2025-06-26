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

        public (int, int) Tuple { get; set; } = (1, 2);

        public (int, int) GetTuple()
            => Tuple;

        public MyParam ParsableValue { get; set; } = new MyParam("1", "2");

        public MyParam GetParsableValue()
            => ParsableValue;
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

        // This class is parsable because static Parse() method is implemented and it can parse result of ToString().
        // Otherwise InstantRpc cannot handle the type as setter value or getter/method returning value.
        // Note that method parameters are not required to be parsable.

        public static MyParam Parse(string value)
            => new MyParam() { Value = value };

        public override string ToString()
            => Value;
    }
}
