using System;
using T4Toolbox;

namespace T4Toolbox.VSHostLites
{
    /// <summary>
    /// Class to change the LogicalCallContext temporarily.
    /// </summary>
    internal class LogicalCallContextChange : IDisposable
    {
        private readonly string _name;
        private readonly object _prevHint;

        /// <summary>
        /// /
        /// </summary>
        /// <param name="name"></param>
        /// <param name="newValue"></param>
        public LogicalCallContextChange(string name, object newValue)
        {
            _name = name;
            _prevHint = CallContext.GetData(name);
            CallContext.SetData(name, newValue);
        }

        /// <summary>
        /// /
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                CallContext.SetData(_name, _prevHint);
            }
        }
    }
}