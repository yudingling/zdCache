using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Remoting.Contexts;
using System.Threading;

namespace ZdCache.MasterCache.Caller
{
    internal enum MREType { ReSet, Set, WaitOne }

    /// <summary>
    /// Call 对应的 ManualResetEvent 控制类。 之所以单独出来，是为了保证同一个上下文
    /// </summary>
    [Synchronization(true)]
    internal class MREContext : ContextBoundObject
    {
        //控制 call 的超时
        private ManualResetEvent manualRE = new ManualResetEvent(false);

        private bool Reset()
        {
            try
            {
                this.manualRE.Reset();
            }
            catch
            {
            }
            return true;
        }

        private bool Set(int allCallCount, int returnedCallCount)
        {
            try
            {
                //如果call 已全部 return，则 set
                if (allCallCount == returnedCallCount)
                {
                    this.manualRE.Set();
                    return true;
                }
            }
            catch
            {
            }
            return false;
        }

        private bool WaitOne(int allCallCount, int returnedCallCount, int millisecondsTimeout)
        {
            try
            {
                //如果无处理process 或者 process 都已返回，则认为是成功的，直接返回 true，不等待
                if (allCallCount <= 0 || allCallCount == returnedCallCount)
                    return true;

                //waitOut 后，注意，必须退出同步上下文
                return this.manualRE.WaitOne(millisecondsTimeout, true);
            }
            catch
            {
                return true;
            }
        }

        /// <summary>
        /// 执行 ManualResetEvent 的操作
        /// </summary>
        /// <param name="allCallCount"></param>
        /// <param name="returnedCallCount"></param>
        /// <param name="millisecondsTimeout">只在 type 为 MREType.WaitOne 时才生效</param>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool Done(int allCallCount, int returnedCallCount, int millisecondsTimeout, MREType type)
        {
            try
            {
                switch (type)
                {
                    case MREType.ReSet:
                        return this.Reset();
                    case MREType.Set:
                        return this.Set(allCallCount, returnedCallCount);
                    case MREType.WaitOne:
                        return this.WaitOne(allCallCount, returnedCallCount, millisecondsTimeout);

                    default:
                        return true;
                }
            }
            catch
            {
                return true;
            }
        }
    }
}
