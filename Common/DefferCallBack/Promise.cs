using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;

namespace ZdCache.Common.DefferCallBack
{
    public class Promise
    {
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
