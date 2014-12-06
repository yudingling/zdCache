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

        private object lockObj = new object();

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
        /// 挂载延迟回调方法，支持链式调用。
        /// 
        ///    todo.此处有个问题没有解决， 即如何保证所有的 then 设置的回调全部被触发。
        ///         存在这种情形:  promiseA.then(...); threadSleep(100); promiseA.then(...);
        ///                       在这种情况下，Emit 方法不能决定何时停止 while 循环， 因为你没办法知道 then 何时不再被调用。
        ///    
        ///            1、一旦阻塞了 Emit 的返回，将阻塞 PorterBase 的回调结束，如果你的回调线程设置为4个，PorterBase 的回调就阻塞在这4个回调上；
        ///            2、只要不存在上面的伪代码中的情形，一般不会被漏，因为 then 的链式调用执行很快，比 Emit 的触发肯定是要先，但不能排除 sb 写法；
        ///      
        ///    脑袋瘦了一圈，没有想到了更好的解决方式...... 求大神
        /// </summary>
        /// <param name="callBacks"></param>
        public Promise Then(params Delegate[] callBacks)
        {
            //此处需要lock，因为异步的原因， Emit 执行完后，就会调用 Dispose 方法，导致 semaphore 不可用
            lock (this.lockObj)
            {
                if (this.semaphore != null && Interlocked.Increment(ref this.curThenCount) <= maxThenCount)
                {
                    if (callBacks != null && callBacks.Length > 0)
                    {
                        ConcurrentQueue<Delegate> queue;
                        foreach (Delegate curDelegate in callBacks)
                        {
                            if (curDelegate != null)
                            {
                                if (this.callBackDic.TryGetValue(curDelegate.GetType(), out queue))
                                    queue.Enqueue(curDelegate);
                            }
                        }
                    }

                    this.semaphore.Release();
                }

                return this;
            }
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
                //回调触发后，最多等待 1s 供 then 方法去增加回调
                int waitTime = 1000;
                while (this.semaphore.Wait(waitTime))
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
                    waitTime = 0;
                }
            }
        }

        #region IDisposable 成员

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            lock (this.lockObj)
            {
                if (this.semaphore != null)
                {
                    this.semaphore.Dispose();
                    this.semaphore = null;
                }
            }
        }

        #endregion
    }
}
