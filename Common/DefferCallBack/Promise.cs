using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;

namespace ZdCache.Common.DefferCallBack
{
    public class Promise
    {
        private ManualResetEvent manualRE = new ManualResetEvent(true);

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
        }

        /// <summary>
        /// 挂载延迟回调方法，支持链式调用
        /// </summary>
        /// <param name="callBacks"></param>
        public Promise Then(params Delegate[] callBacks)
        {
            if (callBacks != null && callBacks.Length > 0)
            {
                ConcurrentQueue<Delegate> queue;
                foreach (Delegate curDelegate in callBacks)
                {
                    if (this.callBackDic.TryGetValue(curDelegate.GetType(), out queue))
                    {
                        queue.Enqueue(curDelegate);

                        //使回调继续执行
                        this.manualRE.Set();
                    }
                }
            }

            return this;
        }

        /// <summary>
        /// 触发回调，按加入的顺序依次触发， 此方法不会抛出回调的异常
        /// </summary>
        internal void Emit(Type callBackType, params object[] args)
        {
            ConcurrentQueue<Delegate> queue;
            if (this.callBackDic.TryGetValue(callBackType, out queue))
            {
                Delegate curDelegate;
                //回调触发后，最多等待 1s 供 then 方法去增加回调。
                //此处这么做是考虑到如果主体完成非常快，导致 then 的链式调用还没全部完成，Emit 就执行了，则需要尽量保证所有的回调执行到
                while (this.manualRE.WaitOne(1000, false))
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
    }
}
