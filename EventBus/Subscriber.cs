using System;
using System.Threading.Tasks;

namespace EventBus
{
    internal abstract class Subscriber
    {
        public Description Description { get; }

        public IThreadHelper ThreadHelper { get; set; }

        public object Source { get; }
        
        protected Subscriber(Description description, object source, IThreadHelper threadHelper)
        {
            Description = description;
            Source = source;
            ThreadHelper = threadHelper;
        }

        protected abstract Action<object> GetExecuteAction();

        public void Execute(object message)
        {
            Execute(GetExecuteAction(), message);
        }

        private void Execute(Action<object> job, object message)
        {
            if (!Description.ParameterType.IsInstanceOfType(message)) return;

            switch (Description.ThreadMode)
            {
                case ThreadMode.Post:
                    ExecuteSubscriber(job, message);
                    break;

                case ThreadMode.Thread:
                    ExecuteSubscriberInBackground(job, message);
                    break;

                case ThreadMode.Async:
                    ExecuteSubscriberAsync(job, message);
                    break;

                case ThreadMode.Main:
                    ExecuteSubscriberInMain(job, message);
                    break;

                case ThreadMode.MainOrder:
                    ExecuteSubscriberInMainOrder(job, message);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ExecuteSubscriberInMainOrder(Action<object> job, object message)
        {
            ThreadHelper.RunInMainThread(() => { ExecuteSubscriber(job, message); }, false);
        }

        private void ExecuteSubscriberInMain(Action<object> job, object message)
        {
            ThreadHelper.RunInMainThread(() => { ExecuteSubscriber(job, message); }, true);
        }

        private void ExecuteSubscriberAsync(Action<object> job, object message)
        {
            Task.Run(() => { ExecuteSubscriber(job, message); });
        }

        private void ExecuteSubscriberInBackground(Action<object> job, object message)
        {
            if (ThreadHelper.IsMainThread())
                ThreadHelper.NewThread(() => { ExecuteSubscriber(job, message); });
            else
                ExecuteSubscriber(job, message);
        }

        private void ExecuteSubscriber(Action<object> job, object message)
        {
            job(message);
        }
    }
}