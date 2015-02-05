using System;
using System.Collections.Generic;
using ZdCache.PorterBase.Setting;
using ZdCache.Common.SizeGetter;
using ZdCache.Common.CDataType;
using ZdCache.MasterCache.Caller;
using ZdCache.Common;
using ZdCache.MasterCache.LoadbalanceStrategy;
using ZdCache.Common.CacheCommon;
using System.Threading.Tasks;
using ZdCache.PorterBase;
using ZdCache.Common.DefferCallBack;

namespace ZdCache.MasterCache
{
    /// <summary>
    /// 缓存 Master
    /// </summary>
    public class Master : Deffered
    {
        private string masterName;
        private Binding myBinding;
        private BalanceHandler balancer;

        /// <summary>
        /// deffered callback 类型
        /// </summary>
        private Type successType = typeof(SuccessInMaster), failType = typeof(FailInMaster);

        public Master(int port, int recvAndSendTimeout)
            : base(ConstParams.CallTimeOut)
        {
            try
            {
                this.masterName = string.Format("_master[port_{0}]_", port);

                SocketServerSettings setting = new SocketServerSettings(
                    port,
                    System.Net.Sockets.ProtocolType.Tcp,
                    recvAndSendTimeout,
                    new DefaultSizeGetter(),
                    Function.GetCpuCoreCount());

                this.myBinding = new Binding(setting, new PorterBase.ErrorTracer(PBLogError));

                //使用默认的负载平衡策略
                this.balancer = new BalanceHandler(this.myBinding, new DefaultLoadBalanceStrategy());

                Logger.WriteLog(LogMsgType.Info, this.masterName,
                    string.Format("master 初始化成功[成功绑定到端口：{0}]！", port));
            }
            catch
            {
                //存在有限资源的分配（base 中分配了线程），如果异常，需要释放资源
                this.Dispose();
                throw;
            }
        }

        

        private void PBLogError(ErrorType errorType, int tokenID, string error)
        {
            Logger.WriteLog(LogMsgType.Error, this.masterName, error);
        }

        public ICollection<StatusInfo> SlaveStatus
        {
            get
            {
                ICollection<StatusInfo> allStaus = new List<StatusInfo>();
                Parallel.ForEach<SlaveModel>(this.myBinding.Slaves, slave =>
                {
                    allStaus.Add(slave.Status);
                });

                return allStaus;
            }
        }

        #region cache action

        /// <summary>
        /// todo. 此功能需要改造，对于返回多个结果集合的情况，则是范围之类的查询条件，而不是一个 ICacheDataType 参数 id去查询，通过 key 查询只能是一个结果)
        /// 查找缓存，多个结果集合
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public IList<ICacheDataType> GetList(ICacheDataType key)
        {
            if (key == null)
                throw new Exception("param 'key' can not be null");

            CallGet cf = new CallGet();
            IList<ICacheDataType> retList;

            cf.Process(this.myBinding.Slaves, key, out retList);

            return retList;
        }

        /// <summary>
        /// 查找缓存
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public ICacheDataType Get(ICacheDataType key)
        {
            if (key == null)
                throw new Exception("param 'key' can not be null");

            CallGet cf = new CallGet(true, null);
            IList<ICacheDataType> retList;

            cf.Process(this.myBinding.Slaves, key, out retList);
            if (retList != null && retList.Count > 0)
                return retList[0];

            return null;
        }

        /// <summary>
        /// 查找缓存，异步
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public Promise GetAsync(ICacheDataType key)
        {
            if (key == null)
                throw new Exception("param 'key' can not be null");

            CallGet cf = new CallGet(false, new FinishedDelegate(this.DefferedCallBack));

            //注意，必须先 CreatePromise 再执行 cf.Process，避免因 cf.Process 回调已经产生，而 promise 确没有创建的情况。 
            Promise promise = this.CreatePromise(cf.ID, this.successType, this.failType);
            cf.Process(this.myBinding.Slaves, key);

            return promise;
        }

        /// <summary>
        /// 设置缓存
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Set(ICacheDataType value)
        {
            if (value == null)
                throw new Exception("param 'value' can not be null");

            CallSet cs = new CallSet(this.balancer);
            IList<ICacheDataType> retList;

            return cs.Process(this.myBinding.Slaves, value, out retList);
        }

        /// <summary>
        /// 更新缓存
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Update(ICacheDataType value)
        {
            if (value == null)
                throw new Exception("param 'value' can not be null");

            CallUpdate cu = new CallUpdate();
            IList<ICacheDataType> retList;

            return cu.Process(this.myBinding.Slaves, value, out retList);
        }

        /// <summary>
        /// 删除缓存
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Delete(ICacheDataType value)
        {
            if (value == null)
                throw new Exception("param 'value' can not be null");

            CallDelete cd = new CallDelete();
            IList<ICacheDataType> retList;

            return cd.Process(this.myBinding.Slaves, value, out retList);
        }

        #endregion

        /// <summary>
        /// deffered 超时
        /// </summary>
        /// <param name="callID"></param>
        protected override void DefferedTimeOut(Guid callID)
        {
            this.Emit(callID, this.failType, new TimeoutException());
        }

        /// <summary>
        /// call 的最终回调
        /// </summary>
        /// <param name="callID"></param>
        /// <param name="success"></param>
        /// <param name="obj">具体的参数，success 为 true 时为 ICollection<ICacheDataType>， false 时为 exception 的实例</param>
        private void DefferedCallBack(Guid callID, bool success, object obj)
        {
            if (success)
                this.Emit(callID, this.successType, obj);
            else
                this.Emit(callID, this.failType, obj);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
            if (this.myBinding != null)
                this.myBinding.Close();

            if (this.balancer != null)
                this.balancer.Dispose();

            base.Dispose();
        }
    }
}
