using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using ZdCache.Common.CacheCommon;
using System.Threading.Tasks;

namespace ZdCache.Common.DefferCallBack
{
    public class Deffered : IDisposable
    {
        private class TimeOutCheckItem
        {
            public Guid ID { get; set; }
            public DateTime TM { get; set; }
        }

        private AsyncCall call;

        private ConcurrentDictionary<Guid, TimeOutCheckItem> timeOutCheckDic = new ConcurrentDictionary<Guid, TimeOutCheckItem>();
        private ConcurrentDictionary<Guid, Promise> promiseDic = new ConcurrentDictionary<Guid, Promise>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="timeOut"></param>
        public Deffered(int timeOut)
        {
            this.call = new AsyncCall(new AsyncMethod(DefferedTimeOutCheck), new AsyncArgs() { Args = timeOut }, true, null);
        }

        /// <summary>
        /// 执行过期检查。
        ///    以轮询的方式去执行，如果某一时刻n多调用都超时，则会出现遍历队列时间过长的问题。但对于实际情况来说，影响不大
        /// </summary>
        private void DefferedTimeOutCheck(AsyncArgs arg)
        {
            int timeOut = (int)arg.Args;
            TimeOutCheckItem itemTmp = null;
            while (true)
            {
                try
                {
                    Parallel.ForEach<TimeOutCheckItem>(this.timeOutCheckDic.Values, item =>
                    {
                        if (((TimeSpan)(DateTime.Now - item.TM)).TotalMilliseconds >= timeOut)
                        {
                            //比如从过期列表中成功移除才能触发 timeOut 事件
                            if (this.timeOutCheckDic.TryRemove(item.ID, out itemTmp))
                            {
                                //PromiseTimeOut 的异常忽略掉
                                try
                                {
                                    this.DefferedTimeOut(item.ID);
                                }
                                catch
                                {
                                }
                            }
                        }
                    });
                }
                catch
                {
                }

                SleepHelper.Sleep(timeOut);
            }
        }

        /// <summary>
        /// 创建 promise
        /// </summary>
        /// <param name="callID"></param>
        /// <param name="callBackTypes"></param>
        /// <returns></returns>
        public Promise CreatePromise(Guid callID, params Type[] callBackTypes)
        {
            Promise promise = null;
            if (!this.promiseDic.TryGetValue(callID, out promise))
            {
                //注意此处的写法，一定要保证数据来自于字典中, 但不要采用 tryadd 紧接着再 tryget，那样在最坏的情况下效率减半
                promise = new Promise(callBackTypes);
                if (this.promiseDic.TryAdd(callID, promise))
                {
                    //加入过期列表
                    this.timeOutCheckDic.TryAdd(callID, new TimeOutCheckItem() { ID = callID, TM = DateTime.Now });
                }
                else
                {
                    promise = null;
                    this.promiseDic.TryGetValue(callID, out promise);
                }
            }

            return promise;
        }

        /// <summary>
        /// 触发回调。
        /// 对于一个 promise 来说，任意一个回调类型被 emit 了，则将从 deffered 中剔除（deffered 状态的唯一性）
        /// </summary>
        public void Emit(Guid callID, Type callBackType, params object[] args)
        {
            TimeOutCheckItem item = null;
            Promise promise = null;

            //执行并删除对应的 promise
            if (this.promiseDic.TryRemove(callID, out promise))
            {
                //从过期列表中移除，注意，必须是从过期列表中移除成功了才能触发 Emit
                if (this.timeOutCheckDic.TryRemove(callID, out item))
                    promise.Emit(callBackType, args);
            }
        }

        /// <summary>
        /// 过期事件
        /// </summary>
        /// <param name="callID"></param>
        protected virtual void DefferedTimeOut(Guid callID) { }

        #region IDisposable 成员

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            if (this.call != null)
                this.call.Stop();
        }

        #endregion
    }
}
