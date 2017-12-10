using System;

namespace EventBus
{
    public interface IInvonker : IDisposable
    {
        void Send(Action action);

        void Post(Action action);
    }
}