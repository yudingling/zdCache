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

namespace ZdCache.MasterCache
{
    /// <summary>
    /// 缓存 Master
    /// </summary>
    public class Master
    {
        private string masterName;
        private Binding myBinding;
        private BalanceHandler balancer;

        public Master(int port, int recvAndSendTimeout)
        {
            this.masterName = string.Format("_master[port_{0}]_", port);

            SocketServerSettings setting = new SocketServerSettings(
                port,
                System.Net.Sockets.ProtocolType.Tcp,
                recvAndSendTimeout,
                new DefaultSizeGetter(),
                4);

            this.myBinding = new Binding(setting, new PorterBase.ErrorTracer(PBLogError));

            //使用默认的负载平衡策略
            this.balancer = new BalanceHandler(this.myBinding, new DefaultLoadBalanceStrategy());

            Logger.WriteLog(LogMsgType.Info, this.masterName,
                string.Format("master 初始化成功[成功绑定到端口：{0}]！", port));
        }

        private void PBLogError(string error)
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
        public List<ICacheDataType> GetList(ICacheDataType key)
        {
            if (key == null)
                throw new Exception("param 'key' can not be null");

            CallGet cf = new CallGet();
            List<ICacheDataType> retList;

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

            CallGet cf = new CallGet(true);
            List<ICacheDataType> retList;

            cf.Process(this.myBinding.Slaves, key, out retList);
            if (retList != null && retList.Count > 0)
                return retList[0];

            return null;
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
            List<ICacheDataType> retList;

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
            List<ICacheDataType> retList;

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
            List<ICacheDataType> retList;

            return cd.Process(this.myBinding.Slaves, value, out retList);
        }

        #endregion
    }
}
