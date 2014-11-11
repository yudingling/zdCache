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

        /// <summary>
        /// 由子类去实现
        /// </summary>
        public virtual void Send(byte[] data) { }

        /// <summary>
        /// 由子类去实现
        /// </summary>
        public virtual void Send(int tokenID, byte[] data) { }

        /// <summary>
        /// 由子类去实现
        /// </summary>
        /// <param name="tokenID"></param>
        public virtual void DropClient(int tokenID) { }

        /// <summary>
        /// 此方法 abstract 掉，强制子类实现
        /// </summary>
        public abstract void Close();

        public void TraceError(ErrorType errorType, int tokenID, string msg)
        {
            if (this.errorTracer != null)
                this.errorTracer(errorType, tokenID, msg);
        }

        #endregion
    }
}
