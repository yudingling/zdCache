using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;

namespace ZdCache.Common.DefferCallBack
{
    public class Promise : IDisposable
    {
        private static int maxThenCount = 10;

        private int curThenCount = 0;
        private SemaphoreSlim semaphore;

        /// <summary>
        /// 存储此 promise 挂载的回调列表。 key 回调类型
        /// </summary>
        private ConcurrentDictionary<Type, ConcurrentQueue<Delegate>> callBackDic = new ConcurrentDictionary<Type, ConcurrentQueue<Delegate>>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="callBackTypes">回调 delegate 的类型列表</param>
        public Promise(params Type[] callBackTypes)
        {
            if (callBackTypes != null && callBackTypes.Length > 0)
            {
                foreach (Type curType in callBackTypes)
                {
                    callBackDic.TryAdd(curType, new ConcurrentQueue<Delegate>());
                }
            }

            this.semaphore = new SemaphoreSlim(0, maxThenCount);
        }

        /// <summary>
        /// 挂载延迟回调方法，支持链式调用
        /// </summary>
        /// <param name="callBacks"></param>
        public Promise Then(params Delegate[] callBacks)
        {
            if (this.semaphore != null && Interlocked.Increment(ref this.curThenCount) <= maxThenCount)
            {
                if (callBacks != null && callBacks.Length > 0)
                {
                    try
                    {
                        ConcurrentQueue<Delegate> queue;
                        foreach (Delegate curDelegate in callBacks)
                        {
                            if (this.callBackDic.TryGetValue(curDelegate.GetType(), out queue))
                            {
                                queue.Enqueue(curDelegate);
                            }
                        }
                    }
                    catch
                    {
                    }
                }

                this.semaphore.Release();
            }

            return this;
        }

        /// <summary>
        /// 触发回调，按加入的顺序依次触发， 此方法不会抛出回调的异常。  
        ///    一个 promise 实例的 emit 方法只会被外部调用一次，见 Deffered 中的实现
        /// </summary>
        internal void Emit(Type callBackType, params object[] args)
        {
            ConcurrentQueue<Delegate> queue;
            if (this.callBackDic.TryGetValue(callBackType, out queue))
            {
                Delegate curDelegate;
                //回调触发后，一次循环中最多等待 1s 供 then 方法去增加回调。
                //此处这么做是考虑到如果主体完成非常快，导致 then 的链式调用还没全部完成，Emit 就执行了，则需要尽量保证所有的回调执行到
                while (this.semaphore.Wait(1000))
                {
                    while (queue.TryDequeue(out curDelegate))
                    {
                        try
                        {
                            curDelegate.DynamicInvoke(args);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        #region IDisposable 成员

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (this.semaphore != null)
            {
                this.semaphore.Dispose();
                this.semaphore = null;
            }
        }

        #endregion
    }
}
