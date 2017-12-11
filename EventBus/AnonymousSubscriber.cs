using System;

namespace EventBus
{
    internal class AnonymousSubscriber<T> : Subscriber
    {
        public AnonymousSubscriber(
            Action<T> action,
            ThreadMode threadMode,
            bool stack,
            IThreadHelper threadHelper)
            : base(Description.FromGeneric<T>(threadMode, stack), action, threadHelper)
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