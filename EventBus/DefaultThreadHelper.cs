using System;
using System.Threading;

namespace EventBus
{
    public sealed class DefaultThreadHelper : IThreadHelper
    {
        private int _mainThreadId;
        private IInvonker _invonker;

        /// <summary>
        /// Must be called in main thread.
        /// </summary>
        public void Initialize()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _invonker = new WinFormInvonker();
        }

        public bool IsMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        }

        public void NewThread(Action job)
        {
            Precondition.ArgumentNotNull(job, nameof(job));
            new Thread(() => { job(); }).Start();
        }

        public void RunInMainThread(Action job, bool wait)
        {
            Precondition.ArgumentNotNull(job, nameof(job));

            try
            {
                var invonker = GetInvoker();

                if (wait)
                    invonker.Send(job);
                else
                    invonker.Post(job);
            }
            catch
            {
                // ignored
            }
        }

        private IInvonker GetInvoker()
        {
            return _invonker ?? (_invonker = new WinFormInvonker());
        }

        public void Dispose()
        {
            _invonker?.Dispose();
        }
    }
}