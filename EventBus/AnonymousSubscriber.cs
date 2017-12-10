using System;

namespace EventBus
{
    internal class AnonymousSubscriber<T> : Subscriber
    {
        public AnonymousSubscriber(
            Action<T> action,
            ThreadMode threadMode,
            IThreadHelper threadHelper)
            : base(Description.FromGeneric<T>(threadMode), action, threadHelper)
        {
        }

        protected override Action<object> GetExecuteAction()
        {
            return message =>
            {
                ((Action<T>)Source)((T)message);
            };
        }
    }
}