using System;

namespace InstantRpc
{
    /// <summary>
    /// Represents a target instance for RPC calls.
    /// </summary>
    public class TargetInstance
    {
        /// <summary>
        /// The instance to which the RPC calls will be directed.
        /// </summary>
        public object Instance { get; }

        /// <summary>
        /// Wraps a setter calling action to be executed on the target instance.
        /// </summary>
        public Action<Action> ActionWrapper { get; }

        /// <summary>
        /// Wraps getter and method calling function to be executed on the target instance.
        /// </summary>
        public Func<Func<object>, object> FuncWrapper { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TargetInstance"/> class.
        /// </summary>
        /// <param name="instance">The instance to which the RPC calls will be directed.</param>
        /// <param name="actionWrapper">Wraps a setter calling action to be executed on the target instance.</param>
        /// <param name="funcWrapper">Wraps getter and method calling function to be executed on the target instance.</param>
        /// <exception cref="ArgumentNullException">instance is null</exception>
        public TargetInstance(object instance, Action<Action> actionWrapper = null, Func<Func<object>, object> funcWrapper = null)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            Instance = instance;
            ActionWrapper = actionWrapper ?? ((a) => a.Invoke());
            FuncWrapper = funcWrapper ?? ((f) => f.Invoke());
        }
    }
}
