using System;

namespace InstantRpc
{
    /// <summary>
    /// Represents a target instance for RPC calls.
    /// </summary>
    public class TargetInstance
    {
        public object Instance { get; }
        public Action<Action> ActionWrapper { get; }
        public Func<Func<object>, object> FuncWrapper { get; }

        public TargetInstance(object instance, Action<Action> actionWrapper = null, Func<Func<object>, object> funcWrapper = null)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            Instance = instance;
            ActionWrapper = actionWrapper ?? ((a) => a.Invoke());
            FuncWrapper = funcWrapper ?? ((f) => f.Invoke());
        }
    }
}
