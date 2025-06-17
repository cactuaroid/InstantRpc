using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace InstantRpc
{
    /// <summary>
    /// Instant RPC client for a specific type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InstantRpcClient<T>
    {
        /// <summary>
        /// Instance ID to distinguish multiple instances of the same type. "" is ok for single instance case.
        /// </summary>
        public string InstanceId { get; }

        /// <summary>
        /// Instant RPC client constructor.
        /// </summary>
        /// <param name="instanceId">Instance ID to distinguish multiple instances of the same type. "" is ok for single instance case.</param>
        public InstantRpcClient(string instanceId = "")
        {
            InstanceId = instanceId;
        }

        /// <summary>
        /// Sets a value to a property or field of the target instance. 'value' must be primitive type or enum.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="expression">Lamda expression for the property of field</param>
        /// <param name="value">value to set</param>
        public void Set<TValue>(Expression<Func<T, TValue>> expression, TValue value)
        {
            Set(expression, () => value);
        }

        /// <summary>
        /// Sets a value to a property or field of the target instance. 'value' can be constructor call including member initialization.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="expression">Lamda expression for the property of field</param>
        /// <param name="value">Lamda expression returning value to set. ex.) () => new MyClass() { A = 1 } </param>
        public void Set<TValue>(Expression<Func<T, TValue>> expression, Expression<Func<TValue>> value)
        {
            var memberName = GetMemberAndMethodPath(expression);
            var arg = SerializeArgument(value.Body);

            Execute("SET", memberName, arg);
        }

        /// <summary>
        /// Gets a value from a property or field of the target instance. The type must be primitive type or enum.
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="expression">>Lamda expression for the property of field</param>
        /// <returns>value from the property or field</returns>
        /// <exception cref="ArgumentException">TValue does not support Parse.</exception>
        public TValue Get<TValue>(Expression<Func<T, TValue>> expression)
        {
            if (!CanParse(typeof(TValue))) { throw new ArgumentException($"{typeof(TValue)} does not support Parse.", nameof(expression)); }

            var memberName = GetMemberAndMethodPath(expression);
            var response = Execute("GET", memberName);

            return Parse<TValue>(response);
        }

        /// <summary>
        /// Invokes a method of the target instance. The return type must be primitive type or enum.
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="expression">Lamda expression for the method</param>
        /// <returns>returning value from the method</returns>
        /// <exception cref="ArgumentException"></exception>
        public TReturn Invoke<TReturn>(Expression<Func<T, TReturn>> expression)
        {
            if (!CanParse(typeof(TReturn))) { throw new ArgumentException($"{typeof(TReturn)} does not support Parse.", nameof(expression)); }

            var response = InvokeImpl(expression);
            return Parse<TReturn>(response);
        }

        /// <summary>
        /// Invokes a method of the target instance.
        /// </summary>
        /// <param name="expression">>Lamda expression for the method</param>
        public void Invoke(Expression<Action<T>> expression)
        {
            InvokeImpl(expression);
        }

        private string InvokeImpl(LambdaExpression expression)
        {
            var parameters = new List<string>();

            if (expression.Body is MethodCallExpression method)
            {
                var methodName = GetMemberAndMethodPath(expression);
                var args = new XElement("args", SerializeArguments(method.Arguments));

                return Execute("INVOKE", methodName, args);
            }
            else
            {
                throw new ArgumentException("expression must be method call", nameof(expression));
            }
        }

        private string Execute(string operation, string memberName, XElement args = null)
        {
            return ExecuteImpl($"{operation}|{typeof(T).AssemblyQualifiedName}|{InstanceId}|{memberName}|{args?.ToString() ?? ""}");
        }

        private string ExecuteImpl(string message)
        {
            using (var pipeClient = new NamedPipeClientStream("InstantRpcPipe"))
            {
                pipeClient.Connect();

                var ss = new StreamString(pipeClient);
                ss.WriteString(message);

                var response = ss.ReadString();

                var elements = response.Split('|');
                var success = bool.Parse(elements[0]);
                var parameter = elements[1]; // returning value or error message

                if (!success) { throw new InvalidOperationException("Operation failed. Detail: " + parameter); }

                return parameter;
            }
        }

        private static XElement SerializeArgument(Expression arg)
        {
            return SerializeArguments(new[] { arg }).Single();
        }

        private static IEnumerable<XElement> SerializeArguments(IReadOnlyCollection<Expression> args)
        {
            // ex. ) new MyClass1(new MyClass2("a"), "b") { A = "c", B = "d" }
            // ->
            // <ctor type="MyClass1">
            //    <ctor type="MyClass2">
            //        <value type="System.String">a</value>
            //    </ctor>
            //    <value type="System.String">b</value>
            //    <init type="System.String" prop="A">c</init>
            //    <init type="System.String" prop="B">d</init>
            // </ctor>

            return args.Select((x) =>
            {
                if (x is NewExpression @new)
                {
                    return 
                        new XElement("ctor",
                            new XAttribute("type", @new.Type.AssemblyQualifiedName),
                            SerializeArguments(@new.Arguments));
                }
                else if (x is MemberInitExpression memberInit)
                {
                    return
                        new XElement("ctor",
                            new XAttribute("type", memberInit.NewExpression.Type.AssemblyQualifiedName),
                            SerializeArguments(memberInit.NewExpression.Arguments),
                            memberInit.Bindings.OfType<MemberAssignment>().Select((m) =>
                                new XElement("init",
                                    new XAttribute("type", (m.Member as PropertyInfo).PropertyType.AssemblyQualifiedName),
                                    new XAttribute("prop", m.Member.Name),
                                    EvaluateExpression(m.Expression))));
                }
                else
                {
                    return
                        new XElement("value",
                            new XAttribute("type", x.Type.AssemblyQualifiedName),
                            EvaluateExpression(x));
                }
            });
        }

        private static string GetMemberName<TValue>(Expression<Func<T, TValue>> expression)
            => (expression.Body is MemberExpression member) ? member.Member.Name : throw new NotSupportedException();

        private static string GetMemberAndMethodPath(LambdaExpression expression)
        {
            var pathBuilder = new StringBuilder();
            Expression currentExpression = expression.Body;

            while (currentExpression != null)
            {
                if (currentExpression is MemberExpression memberExpression)
                {
                    // property call
                    if (pathBuilder.Length > 0)
                    {
                        pathBuilder.Insert(0, ".");
                    }
                    pathBuilder.Insert(0, memberExpression.Member.Name);
                    currentExpression = memberExpression.Expression;
                }
                else if (currentExpression is MethodCallExpression methodCallExpression)
                {
                    // method call
                    if (pathBuilder.Length > 0)
                    {
                        pathBuilder.Insert(0, ".");
                    }
                    pathBuilder.Insert(0, methodCallExpression.Method.Name);
                    currentExpression = methodCallExpression.Object;
                }
                else if (currentExpression is ParameterExpression)
                {
                    // reached to parameter of lambda, goal!
                    break;
                }
                else if (currentExpression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
                {
                    // cast
                    currentExpression = unaryExpression.Operand;
                }
                else
                {
                    throw new NotSupportedException($"Unsupported expression type: {currentExpression.NodeType}");
                }
            }

            return pathBuilder.ToString();
        }

        private static object EvaluateExpression(Expression expression)
        {
            if (expression is ConstantExpression constant)
            {
                // constant
                var value = constant.Value;

                if (!CanParse(value.GetType())) { throw new ArgumentException($"{value.GetType()} does not support Parse."); }

                return value;
            }
            else
            {
                // variable requires invoke to evaluate
                var lambda = Expression.Lambda<Func<object>>(Expression.Convert(expression, typeof(object)));
                var value = lambda.Compile().Invoke();

                if (!CanParse(value.GetType())) { throw new ArgumentException($"{value.GetType()} does not support Parse."); }

                return value;
            }
        }

        private static bool CanParse(Type type)
        {
            if (type == typeof(string)) { return true; }
            if (type.IsEnum) { return true; }

            var parseMethod = type.GetMethod("Parse", new Type[] { typeof(string) });
            return parseMethod != null;            
        }

        private static TValue Parse<TValue>(string value)
        {
            if (typeof(TValue) == typeof(string)) { return (TValue)(object)value; }
            if (typeof(TValue).IsEnum) { return (TValue)Enum.Parse(typeof(TValue), value); }

            var parseMethod = typeof(TValue).GetMethod("Parse", new Type[] { typeof(string) });
            if (parseMethod is null) { throw new NotSupportedException($"'Parse(string)' is not implemented on type [{typeof(TValue)}]."); }

            return (TValue)parseMethod.Invoke(null, new[] { value });
        }
    }
}
