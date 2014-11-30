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
            Promise promiseTmp = null;
            while (true)
            {
                try
                {
                    Parallel.ForEach<TimeOutCheckItem>(this.timeOutCheckDic.Values, item =>
                    {
                        if (((TimeSpan)(DateTime.Now - item.TM)).TotalMilliseconds >= timeOut)
                        {
                            //注意此处的判断顺序，先 timeOutCheckDic、再 promiseDic， 后续的回调触发中也应该是这个顺序
                            if (this.timeOutCheckDic.TryRemove(item.ID, out itemTmp)
                                && this.promiseDic.TryRemove(item.ID, out promiseTmp))
                            {
                                promiseTmp.Dispose();

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

            //注意此处的判断顺序，和timeout 中的判断顺序必须一致
            if (this.timeOutCheckDic.TryRemove(callID, out item)
                && this.promiseDic.TryRemove(callID, out promise))
            {
                promise.Emit(callBackType, args);
                promise.Dispose();
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
