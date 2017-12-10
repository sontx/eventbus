using System;

namespace EventBus
{
    public interface IThreadHelper : IDisposable
    {
        bool IsMainThread();

        void NewThread(Action job);

        void RunInMainThread(Action job, bool wait);
    }
}