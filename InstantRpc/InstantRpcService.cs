﻿using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InstantRpc
{
    /// <summary>
    /// Instant RPC service for inter-process communication. Call Expose method to expose a target object.
    /// </summary>
    public static class InstantRpcService
    {
        /// <summary>
        /// A dictionary of exposed targets. The key is a tuple of type name and instance ID, and the value is a TargetInstance.
        /// </summary>
        public static IReadOnlyDictionary<(string TypeName, string Key), TargetInstance> Targets => _targets;

        private static Dictionary<(string TypeName, string Key), TargetInstance> _targets = new Dictionary<(string, string), TargetInstance>();
        private static Task _serverThread;
        private static readonly Mutex _processMutex = new Mutex(false, "Global\\InstantRpcService_Mutex");

        // static class is not supported yet

        /// <summary>
        /// Expose a target object for remote access.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">target instance</param>
        /// <param name="instanceId">Instance ID to distinguish multiple instances of the same type. "" is ok for single instance case.</param>
        /// <exception cref="ArgumentException">Specified instanceId is already exposed for the same type</exception>
        /// <exception cref="InvalidOperationException">Another process with InstantRpcService is already running in this PC.</exception>
        public static void Expose<T>(T target, string instanceId = "")
            => Expose(target, "", null, null);

        /// <summary>
        /// Expose a target object for remote access with action and function wrappers. 
        /// Specify '(x) => Dispatcher.Invoke(x)' as wrappers if you want to call operations on the UI thread in WPF applications.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">target instance</param>
        /// <param name="actionWrapper">action wrapper to call Set operation</param>
        /// <param name="funcWrapper">function wrapper to call Get and Invoke operation</param>
        /// <exception cref="ArgumentException">Specified instanceId is already exposed for the same type</exception>
        /// <exception cref="InvalidOperationException">Another process with InstantRpcService is already running in this PC.</exception>
        public static void Expose<T>(T target, Action<Action> actionWrapper, Func<Func<object>, object> funcWrapper)
            => Expose(target, "", actionWrapper, funcWrapper);

        /// <summary>
        /// Expose a target object for remote access with action and function wrappers. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">target instance</param>
        /// <param name="instanceId">Instance ID to distinguish multiple instances of the same type. "" is ok for single instance case.</param>
        /// <param name="actionWrapper">action wrapper to call Set operation</param>
        /// <param name="funcWrapper">function wrapper to call Get and Invoke operation</param>
        /// <exception cref="ArgumentException">Specified instanceId is already exposed for the same type</exception>
        /// <exception cref="InvalidOperationException">Another process with InstantRpcService is already running in this PC.</exception>
        public static void Expose<T>(T target, string instanceId, Action<Action> actionWrapper, Func<Func<object>, object> funcWrapper)
        {
            if (!_processMutex.WaitOne(0)) { throw new InvalidOperationException("Another process with InstantRpcService is already running in this PC."); }

            var key = (typeof(T).AssemblyQualifiedName, instanceId);
            if (_targets.ContainsKey(key))
            {
                throw new ArgumentException($"Duplicated instanceId [{key}] for type {typeof(T)}", nameof(instanceId));
            }
            
            _targets[key] = new TargetInstance(target, actionWrapper, funcWrapper);

            if (_serverThread is null)
            {
                _serverThread = Task.Run(() => ServerThread());
            }
        }

        private static void ServerThread()
        {
            while (true)
            {
                using (var pipeServer = new NamedPipeServerStream("InstantRpcPipe"))
                {
                    pipeServer.WaitForConnection();
                    string response;

                    var ss = new StreamString(pipeServer);

                    var received = ss.ReadString();

                    {
                        // command string format
                        // 　command name|target type|target instance ID|parameters
                        // ex.
                        // 　"GET|MyApp.MainWindow||Top|"
                        // 　"SET|MyApp.MainWindow||Top|<value type="System.Double">10</value>"
                        // 　"INVOKE|MyApp.MainWindow||Do|<value type="System.String">test1</value><value type="System.String">test2</value>"

                        var elements = received.Split('|');
                        var command = elements[0];
                        var type = elements[1];
                        var key = elements[2];
                        var member = elements[3];
                        var args = elements[4];

                        if (command == "WAITFOR")
                        {
                            response = $"{true}|{Targets.ContainsKey((type, key))}";
                        }
                        else if (!Targets.ContainsKey((type, key)))
                        {
                            response = $"{false}|{(type, key)} is not exposed.";
                        }
                        else
                        {
                            var target = Targets[(type, key)];

                            try
                            {
                                switch (command)
                                {
                                    case "GET":
                                        response = Get(target, member);
                                        break;
                                    case "SET":
                                        response = Set(target, member, args);
                                        break;
                                    case "INVOKE":
                                        response = Invoke(target, member, args);
                                        break;
                                    default:
                                        throw new NotSupportedException();
                                }
                            }
                            catch (Exception ex)
                            {
                                response = $"{false}|{ex}";
                            }
                        }

                        var write = ss.WriteString(response);

                        pipeServer.WaitForPipeDrain();
                    }
                }
            }
        }

        private static string Get(TargetInstance target, string memberPath)
        {
            var (instance, memberName) = ExtractPath(target, memberPath);
            var prop = instance.GetType().GetProperty(memberName);
            if (prop is null)
            {
                return $"{false}|Property '{memberName}' not found on type '{instance.GetType()}'.";
            }

            var result = target.FuncWrapper.Invoke(() => prop.GetValue(instance));

            return $"{true}|{result}";
        }

        private static string Set(TargetInstance target, string memberPath, string arg)
        {
            var (instance, memberName) = ExtractPath(target, memberPath);
            var prop = instance.GetType().GetProperty(memberName);
            if (prop is null)
            {
                return $"{false}|Property '{memberName}' not found on type '{instance.GetType()}'.";
            }

            var argXml = XElement.Parse(arg);

            var param = DeserializeArgument(argXml);
            target.ActionWrapper.Invoke(() => prop.SetValue(instance, param));

            return $"{true}|";
        }

        private static string Invoke(TargetInstance target, string memberPath, string args)
        {
            var (instance, memberName) = ExtractPath(target, memberPath);
            var argsXml = XElement.Parse(args);
            var param = DeserializeArguments(argsXml.Elements());

            var method = instance.GetType().GetMethod(memberName, argsXml.Elements().Select((x) => GetType(x)).ToArray());
            if (method is null)
            {
                return $"{false}|Method '{memberName}' not found on type '{instance.GetType()}'.";
            }

            var result = target.FuncWrapper.Invoke(() => method.Invoke(instance, param));

            return $"{true}|{result}";
        }

        private static (object Instance, string MemberName) ExtractPath(TargetInstance target, string memberPath)
        {
            var members = memberPath.Split('.');
            var currentInstance = target.Instance;
            for (var i = 0; i < members.Length - 1; i++)
            {
                currentInstance = target.FuncWrapper.Invoke(() =>
                    currentInstance.GetType().GetProperty(members[i]).GetValue(currentInstance));
            }

            return (currentInstance, members.Last());
        }

        private static object DeserializeArgument(XElement element)
        {
            return DeserializeArguments(new[] { element }).Single();
        }

        private static object[] DeserializeArguments(IEnumerable<XElement> elements)
        {
            return elements.Select((x) =>
            {
                var type = GetType(x);
                if (x.Name == "ctor")
                {
                    // call constructor
                    var argsXml = x.Elements().Where((e) => e.Name == "ctor" || e.Name == "value");
                    var args = DeserializeArguments(argsXml);
                    var instance = Activator.CreateInstance(type, args);

                    // initialize properties
                    foreach (var init in x.Elements("init"))
                    {
                        type
                        .GetProperty(init.Attribute("prop").Value)
                        .SetValue(instance, Parse(init));
                    }

                    return instance;
                }
                else // value
                {
                    return Parse(x);
                }
            }).ToArray();
        }

        private static Type GetType(XElement element)
        {
            return Type.GetType(element.Attribute("type").Value);
        }

        private static object Parse(XElement element)
        {
            return Parser.Parse(GetType(element), element.Value);
        }
    }
}
