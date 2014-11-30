using System;
using System.Runtime.Remoting.Contexts;
using System.Threading;

namespace ZdCache.MasterCache.Caller
{
    internal class WaitingContext : IDisposable
    {
        private int initCallCount = -1;
        private int returnedCallCount = 0;

        private ManualResetEventSlim initCallCountSetedMRE;
        private ManualResetEventSlim waitMRE;

        public WaitingContext()
        {
            this.initCallCountSetedMRE = new ManualResetEventSlim(false);
            this.waitMRE = null;
        }

        /// <summary>
        /// 开始等待
        /// </summary>
        public bool StartWaiting(int callCount, int millisecondsTimeout)
        {
            if (callCount <= 0)
                return true;

            if (this.initCallCount == -1)
            {
                this.waitMRE = new ManualResetEventSlim(false);
                this.initCallCount = callCount;

                //标识 initCallCount 已经设置完成
                this.initCallCountSetedMRE.Set();

                //等待
                return this.waitMRE.Wait(millisecondsTimeout);
            }
            else
                return true;
        }

        /// <summary>
        /// 回调发生
        /// </summary>
        /// <returns>是否所有回调都已产生</returns>
        public bool CallBackHappend()
        {
            //等待直到 initCallCount 设置完成
            if (this.initCallCountSetedMRE.Wait(Timeout.Infinite))
            {
                int temp = Interlocked.Increment(ref this.returnedCallCount);
                if (temp == this.initCallCount)
                {
                    if (this.waitMRE != null)
                        this.waitMRE.Set();
                    return true;
                }
            }

            return false;
        }


        #region IDisposable 成员

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (this.initCallCountSetedMRE != null)
            {
                this.initCallCountSetedMRE.Dispose();
                this.initCallCountSetedMRE = null;
            }

            if (this.waitMRE != null)
            {
                this.waitMRE.Dispose();
                this.waitMRE = null;
            }
        }

        #endregion
    }
}
