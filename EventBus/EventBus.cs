using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventBus
{
    /// <summary>
    /// The publisher/subscriber pattern for loose coupling.
    /// EventBus enables central communication to decoupled classes 
    /// with just a few lines of code – simplifying the code, 
    /// removing dependencies, and speeding up app development.
    /// </summary>
    public sealed class EventBus
    {
        public static EventBus Default { get; } = new EventBus();

        /// <summary>
        /// Subscriber types - subscribers
        /// </summary>
        private readonly Dictionary<Type, IList<Subscriber>> _subscriberList;

        private readonly IList<object> _messageStacked;

        private IThreadHelper _threadHelper;

        /// <summary>
        /// Must be called in main thread.
        /// </summary>
        public void Initialize()
        {
            if (_threadHelper is DefaultThreadHelper defaultThreadHelper)
                defaultThreadHelper.Initialize();
        }

        public void SetThreadHelper(IThreadHelper threadHelper)
        {
            Precondition.ArgumentNotNull(threadHelper, nameof(threadHelper));

            _threadHelper?.Dispose();
            _threadHelper = threadHelper;

            lock (_subscriberList)
            {
                foreach (var node in _subscriberList)
                {
                    foreach (var subscriber in node.Value)
                    {
                        subscriber.ThreadHelper = threadHelper;
                    }
                }
            }
        }

        /// <summary>
        /// Register a container to listen to incomming messages, container is an instance of a class
        /// that contains subscriber methods which annotated by <see cref="SubscribeAttribute"/>.
        /// The subscriber methods must have only one argument with argument type as same as the incomming
        /// message type which wants to receive.
        /// </summary>
        /// <param name="container">
        /// Instance of a class that contains subsciber methods. In most cases, the container
        /// is "this" keyword that mean you want to register current instance of current
        /// class.
        /// </param>
        public void Register(object container)
        {
            Precondition.ArgumentNotNull(container, nameof(container));

            lock (_subscriberList)
            {
                if (_subscriberList.Any(node => node.Value.Any(subscriber => subscriber.Source == container))) return;
                var subscribers = CollectSubscribers(container);
                if (subscribers == null || subscribers.Count == 0) return;
                foreach (var subscriber in subscribers)
                {
                    RegisterSubscriber(subscriber);
                }
            }
        }

        /// <summary>
        /// Register a <see cref="Action{T}"/> to listen to an incomming message.
        /// You should keep the instance of the <see cref="Action{T}"/> to unregister
        /// later if you need.
        /// </summary>
        /// <typeparam name="T">
        /// Type of incomming message which wants to receive.
        /// </typeparam>
        /// <param name="subscriberAction">
        /// The <see cref="Action{T}"/> will be called when the incomming message is reached.
        /// The message data will be passed to the argument of <see cref="subscriberAction"/>.
        /// </param>
        /// <param name="threadMode">
        /// Configure thead mode, see <see cref="ThreadMode"/> for more detail.
        /// </param>
        /// <param name="stack">
        /// If it's true, it itents to receive stacked messages.
        /// </param>
        public void Register<T>(Action<T> subscriberAction, ThreadMode threadMode, bool stack)
        {
            Precondition.ArgumentNotNull(subscriberAction, nameof(subscriberAction));

            lock (_subscriberList)
            {
                var subscriber = new AnonymousSubscriber<T>(subscriberAction, threadMode, stack, _threadHelper);
                RegisterSubscriber(subscriber);
            }
        }

        /// <seealso cref="Register{T}(Action{T}, ThreadMode, bool)"/>
        public void Register<T>(Action<T> subscriberAction, ThreadMode threadMode)
        {
            Precondition.ArgumentNotNull(subscriberAction, nameof(subscriberAction));
            Register(subscriberAction, threadMode, false);
        }

        /// <seealso cref="Register{T}(Action{T}, ThreadMode, bool)"/>
        public void Register<T>(Action<T> subscriberAction, bool stack)
        {
            Precondition.ArgumentNotNull(subscriberAction, nameof(subscriberAction));
            Register(subscriberAction, ThreadMode.Post, stack);
        }

        /// <seealso cref="Register{T}(Action{T}, ThreadMode, bool)"/>
        public void Register<T>(Action<T> subscriberAction)
        {
            Precondition.ArgumentNotNull(subscriberAction, nameof(subscriberAction));
            Register(subscriberAction, ThreadMode.Post, false);
        }

        /// <summary>
        /// Unregister a subscriber source.
        /// </summary>
        /// <param name="source">
        /// Source can be a container or a simple subscriber action
        /// that was reigstered before.
        /// </param>
        public void Unregister(object source)
        {
            Precondition.ArgumentNotNull(source, nameof(source));

            lock (_subscriberList)
            {
                var needToRemovedList = new List<Type>();

                foreach (var node in _subscriberList)
                {
                    for (var j = 0; j < node.Value.Count; j++)
                    {
                        var subscriber = node.Value[j];
                        if (subscriber.Source != source) continue;
                        node.Value.RemoveAt(j);
                        j--;
                    }

                    if (node.Value.Count == 0)
                    {
                        needToRemovedList.Add(node.Key);
                    }
                }

                foreach (var subscriberType in needToRemovedList)
                {
                    _subscriberList.Remove(subscriberType);
                }
            }
        }

        /// <summary>
        /// Broadcast the message to subscribers.
        /// </summary>
        /// <param name="message">
        /// Message data that will be broadcasted.
        /// </param>
        /// <param name="stack">
        /// If the subscribers don't have compatible types it will be stacked in a list.
        /// After that, if there are any subscriber that is registered,
        /// have a compatible type with this message and intents to receive
        /// stacked messages, this message and the others that have a compatible type
        /// in stacked list will be passed to those subscribers. By default, the message
        /// will be ignored if don't have any subscribers which have compatible types.
        /// </param>
        public void Post(object message, bool stack = false)
        {
            Precondition.ArgumentNotNull(message, nameof(message));

            var consumed = false;

            lock (_subscriberList)
            {
                foreach (var node in _subscriberList)
                {
                    if (!node.Key.IsInstanceOfType(message)) continue;
                    foreach (var subscriber in node.Value)
                    {
                        subscriber.Execute(message);
                        consumed = true;
                    }
                }
            }

            if (consumed || !stack) return;

            lock (_messageStacked)
            {
                _messageStacked.Add(message);
            }
        }

        private void RegisterSubscriber(Subscriber subscriber)
        {
            foreach (var node in _subscriberList)
            {
                if (!node.Key.IsAssignableFrom(subscriber.Description.ParameterType)) continue;
                node.Value.Add(subscriber);
                return;
            }

            _subscriberList.Add(subscriber.Description.ParameterType, new List<Subscriber>(new[] { subscriber }));

            if (!subscriber.Description.Stack) return;

            lock (_messageStacked)
            {
                for (var i = 0; i < _messageStacked.Count; i++)
                {
                    var message = _messageStacked[i];
                    if (!subscriber.Description.ParameterType.IsInstanceOfType(message)) continue;

                    _messageStacked.RemoveAt(i--);
                    subscriber.Execute(message);
                }
            }
        }

        private IList<Subscriber> CollectSubscribers(object container)
        {
            var type = container.GetType();
            var methodInfos = type.GetRuntimeMethods();
            var subscribers = new List<Subscriber>();

            foreach (var methodInfo in methodInfos)
            {
                var subscriberAttribute = methodInfo.GetCustomAttribute<SubscribeAttribute>();
                if (subscriberAttribute == null) continue;
                var subscriber = new IdentifiedSubscriber(methodInfo, container, _threadHelper);
                subscribers.Add(subscriber);
            }

            return subscribers;
        }

        public EventBus()
        {
            _subscriberList = new Dictionary<Type, IList<Subscriber>>();
            _messageStacked = new List<object>();
            _threadHelper = new DefaultThreadHelper();
        }
    }
}