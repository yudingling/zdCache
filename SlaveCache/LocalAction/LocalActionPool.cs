using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using ZdCache.Common.CDataType;
using System.Collections.Generic;
using ZdCache.Common;

namespace ZdCache.SlaveCache.LocalAction
{
    /// <summary>
    /// 本地化操作缓存池
    /// </summary>
    public class LocalActionPool : IDisposable
    {
        #region innerclass， 操作信息

        class LlAcInfo
        {
            public LocalActionType LocalAcType { get; set; }
            public ICacheDataType Data { get; set; }
        }

        #endregion

        private bool running = true;

        private ConcurrentQueue<LlAcInfo> pool = new ConcurrentQueue<LlAcInfo>();
        private List<AsyncCall> callList = new List<AsyncCall>();

        private ILocalAction myLocalActor;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="localActionExecThreadCount">执行本地化操作的线程数</param>
        public LocalActionPool(int localActionExecThreadCount, ILocalAction localActor)
        {
            this.myLocalActor = localActor;

            for (int i = 0; i < localActionExecThreadCount; i++)
            {
                callList.Add(new AsyncCall(new AsyncMethod(DoLocalAction), null, true, null));
            }
        }

        public void Push(LocalActionType localAcType, ICacheDataType data)
        {
            if (running)
                this.pool.Enqueue(new LlAcInfo() { LocalAcType = localAcType, Data = data });
        }

        private void DoLocalAction(AsyncArgs arg)
        {
            while (true)
            {
                try
                {
                    if (this.pool.Count > 0)
                    {
                        LlAcInfo info;
                        while (this.pool.TryDequeue(out info))
                        {
                            this.myLocalActor.ExecLocalAction(info.LocalAcType, info.Data);
                        }
                    }
                    else
                        SleepHelper.Sleep(100);
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(LogMsgType.Error, "[LocalActionPool.DoLocalAction] 异常:" + ex.Message + " " + ex.StackTrace);
                }
            }
        }

        #region IDisposable 成员

        /// <summary>
        /// 释放资源，阻塞，直到所有的本地化操作完成
        /// </summary>
        public void Dispose()
        {
            this.running = false;
            while (this.pool.Count > 0)
                SleepHelper.Sleep(100);

            foreach (AsyncCall call in this.callList)
                call.Stop();
        }

        #endregion
    }
}
