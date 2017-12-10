using System;
using System.Globalization;
using System.Reflection;

namespace EventBus
{
    internal class IdentifiedSubscriber : Subscriber
    {
        private readonly MethodInfo _subscriberMethodInfo;

        public IdentifiedSubscriber(
            MethodInfo subscriberMethodInfo,
            object container,
            IThreadHelper threadHelper)
            : base(Description.FromMethodInfo(subscriberMethodInfo), container, threadHelper)
        {
            _subscriberMethodInfo = subscriberMethodInfo;
        }

        protected override Action<object> GetExecuteAction()
        {
            return message =>
            {
                _subscriberMethodInfo.Invoke(
                    Source,
                    BindingFlags.Instance | BindingFlags.InvokeMethod,
                    null,
                    new[] { message },
                    CultureInfo.CurrentCulture);
            };
        }
    }
}