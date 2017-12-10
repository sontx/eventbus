using System;
using System.Windows.Forms;

namespace EventBus
{
    public class WinFormInvonker : IInvonker
    {
        private readonly Control _control;

        public WinFormInvonker()
        {
            _control = new Control();
            _control.CreateControl();
        }

        public void Send(Action action)
        {
            if (_control.IsDisposed || _control.Disposing) return;
            _control.Invoke((MethodInvoker)delegate { action(); });
        }

        public void Post(Action action)
        {
            if (_control.IsDisposed || _control.Disposing) return;
            _control.BeginInvoke((MethodInvoker)delegate { action(); });
        }

        public void Dispose()
        {
            _control?.Dispose();
        }
    }
}