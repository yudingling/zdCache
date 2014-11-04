using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.PorterBase
{
    public abstract class Porter : IPorter
    {
        private ErrorTracer errorTracer;

        public Porter(ErrorTracer tracer)
        {
            this.errorTracer = tracer;
        }

        #region IPorter 成员

        public abstract void Send(byte[] data);

        public abstract void Send(int tokenID, byte[] data);

        public abstract void Close();

        public void TraceError(string msg)
        {
            if (this.errorTracer != null)
                this.errorTracer(msg);
        }

        #endregion
    }
}
