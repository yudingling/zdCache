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

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="isSync">是否同步</param>
        public WaitingContext(bool isSync)
        {
            this.initCallCountSetedMRE = new ManualResetEventSlim(false);
            this.waitMRE = isSync ? new ManualResetEventSlim(false) : null;
        }

        /// <summary>
        /// 开始等待
        /// </summary>
        public bool StartWaiting(int callCount, int millisecondsTimeout)
        {
            try
            {
                if (callCount <= 0)
                    return true;

                if (this.initCallCount == -1)
                {
                    this.initCallCount = callCount;

                    //标识 initCallCount 已经设置完成
                    this.initCallCountSetedMRE.Set();

                    //等待
                    if (this.waitMRE != null)
                        return this.waitMRE.Wait(millisecondsTimeout);
                }
            }
            catch
            {
                //因 StartWaiting 与 CallBackHappend 异步的关系，CallBackHappend 返回true后，
                //在 Call.DoFinishProcess 中会调用该 Dispose 方法，导致 this.waitMRE.Wait 异常
            }

            return true;
        }

        /// <summary>
        /// 回调发生
        /// </summary>
        /// <returns>是否所有回调都已产生</returns>
        public bool CallBackHappend()
        {
            try
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
            }
            catch
            {
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
