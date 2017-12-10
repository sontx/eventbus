using System;
using System.ComponentModel;

namespace EventBus
{
    public class AsyncOperationInvonker : IInvonker
    {
        private readonly AsyncOperation _asyncOperation;

        public AsyncOperationInvonker()
        {
            _asyncOperation = AsyncOperationManager.CreateOperation(null);
        }

        public void Send(Action action)
        {
            _asyncOperation.SynchronizationContext.Send(state => { action(); }, null);
        }

        public void Post(Action action)
        {
            _asyncOperation.SynchronizationContext.Post(state => { action(); }, null);
        }

        public void Dispose()
        {
        }
    }
}