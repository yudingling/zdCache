using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.PorterBase.Setting;
using ZdCache.Common.SizeGetter;
using ZdCache.Common.CDataType;
using ZdCache.Common;
using ZdCache.Common.ActionModels;
using ZdCache.SlaveCache.PassiveExpireStrategy;
using ZdCache.Common.CacheCommon;
using ZdCache.PorterBase;

namespace ZdCache.SlaveCache
{
    public class Slave : IDisposable
    {
        //slave 的唯一标识
        private Guid slaveID = Guid.NewGuid();

        private MasterDispatcher dispatcher;

        private APStatusHunter statusHunter;

        private DataPool dataPool;

        private IPassiveExpireStrategy poolExpireStrategy;

        private SlaveCfg slaveCfg;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Slave(SlaveCfg cfg)
        {
            this.slaveCfg = cfg;

            //0、分发器
            this.dispatcher = new MasterDispatcher();

            //1、过期策略
            this.poolExpireStrategy = new DefaultPassiveExpireStrategy(cfg.MaxCacheSize);

            //2、创建 dataPool
            this.dataPool = new DataPool(cfg.CacheExpireTM, cfg.CallBackThreadCount);

            //3、创建状态获取器
            this.statusHunter = new APStatusHunter(new StatusDataArrived(SendStatusDataAction));

            //初始化放到线程中，因为可能会遇到 socket 连接不上等错误
            AsyncCall initCall = new AsyncCall(new AsyncMethod(this.Init), null, true, null);
        }

        private void Init(AsyncArgs arg)
        {
            GenerateBinding(this.slaveCfg.Masters.Values, this.slaveCfg.RecvAndSendTimeout, this.slaveCfg.CallBackThreadCount);
        }



        /// <summary>
        /// 向服务端注册此 slave， 实际上是发送一个 statusInfo
        /// </summary>
        private void CallRegisterAction(ClientBinding binding)
        {
            StatusInfo status = new StatusInfo(this.slaveID);
            CacheSerializableObject cachedObj = new CacheSerializableObject(string.Empty, status);

            CallArgsModel regArgModel = new CallArgsModel(Guid.NewGuid(), ActionKind.APStatusInfo, cachedObj);

            CommonFunc.SendCall(binding, regArgModel);
        }

        /// <summary>
        /// APStatusHunter 回调
        /// </summary>
        /// <param name="info"></param>
        private void SendStatusDataAction(StatusInfo info)
        {
            //重要，slave 的唯一标识指定 
            info.SlaveRegisterID = this.slaveID;
            //StatusInfo 的 CachedMem、HitRate 设置
            info.CachedMem = this.dataPool.CachedMemSize;
            info.HitRate = this.dataPool.HitRate;

            //执行被动缓存失效策略
            ExecPassiveExpireStrategy(info);

            CacheSerializableObject cachedObj = new CacheSerializableObject(string.Empty, info);

            CallArgsModel callModel = new CallArgsModel(Guid.NewGuid(), ActionKind.APStatusInfo, cachedObj);

            this.dispatcher.Dispatch(callModel);
        }

        private void GenerateBinding(ICollection<SlaveCfg.IpAndPort> ipAndPorts, int recvAndSendTimeout, int callBackThreadCount)
        {
            bool successed = false;
            List<string> successIds = new List<string>();

            while (!successed)
            {
                try
                {
                    foreach (SlaveCfg.IpAndPort item in ipAndPorts)
                    {
                        if (successIds.Contains(item.ID))
                            continue;

                        ClientBinding binding = null;
                        try
                        {
                            //0、从 dispatcher 中移除
                            this.dispatcher.DeleteBinding(item.ID);

                            SocketClientSettings sc = new SocketClientSettings(
                                item.IP,
                                item.Port,
                                System.Net.Sockets.ProtocolType.Tcp,
                                recvAndSendTimeout,
                                new DefaultSizeGetter(),
                                callBackThreadCount);

                            //1、创建 binding
                            binding = new ClientBinding(item.ID, sc,
                                new DataFromBinding(this.CallFromMaster), new SendErrorFromBinding(this.OnSendError));

                            //2、注册
                            CallRegisterAction(binding);

                            //3、加入 dispatcher
                            this.dispatcher.AddBinding(binding);

                            Logger.WriteLog(LogMsgType.Info,
                                string.Format("slave 成功注册到[{0}:{1}]", item.IP, item.Port));

                            successIds.Add(item.ID);

                            if (successIds.Count == ipAndPorts.Count)
                                successed = true;
                        }
                        catch (Exception ex)
                        {
                            if (binding != null)
                                binding.Close();

                            Logger.WriteLog(LogMsgType.Error,
                                string.Format("slave 注册[{0}:{1}] 失败! : {2} {3}", item.IP, item.Port, ex.Message, ex.StackTrace));
                        }
                    }
                }
                catch
                {
                }

                //2s重试
                SleepHelper.Sleep(2000);
            }
        }

        /// <summary>
        /// 执行缓存策略
        /// </summary>
        private void ExecPassiveExpireStrategy(StatusInfo info)
        {
            bool isForceToReduceSize;
            if (this.poolExpireStrategy.ExecStrategy(info, out isForceToReduceSize))
            {
                this.dataPool.ExpireFromStrategy(isForceToReduceSize);
            }
        }

        /// <summary>
        /// 从binding 接收到数据后的回调
        /// </summary>
        /// <param name="model"></param>
        private void CallFromMaster(string bindingID, CallArgsModel model)
        {
            CallArgsModel returnModel = new CallArgsModel(model.AcCallID, model.AcKind, new CacheNull());
            bool success = false;

            switch (model.AcKind)
            {
                case ActionKind.Set:
                    success = this.dataPool.Set(model.AcArgs);
                    returnModel.AcResult = success ? ActionResult.Succeed : ActionResult.Fail;
                    break;

                case ActionKind.Get:
                    ICacheDataType temp = this.dataPool.Get(model.AcArgs);
                    if (temp != null)
                    {
                        returnModel.AcArgs = temp;
                        returnModel.AcResult = ActionResult.Succeed;
                    }
                    else
                        returnModel.AcResult = ActionResult.Fail;
                    break;

                case ActionKind.Delete:
                    success = this.dataPool.Delete(model.AcArgs);
                    returnModel.AcResult = success ? ActionResult.Succeed : ActionResult.Fail;
                    break;

                case ActionKind.Update:
                    success = this.dataPool.Update(model.AcArgs);
                    returnModel.AcResult = success ? ActionResult.Succeed : ActionResult.Fail;
                    break;
            }

            //发送 returnModel
            this.dispatcher.Dispatch(bindingID, returnModel);
        }

        /// <summary>
        /// 发送错误时的回调
        /// </summary>
        private void OnSendError(string bindingId)
        {
            //如果发送失败，则表示与 master 的连接可能断开了，需要重新注册
            try
            {
                List<SlaveCfg.IpAndPort> ipAndPorts = new List<SlaveCfg.IpAndPort>();

                //加入到重新生成的列表中
                ipAndPorts.Add(this.slaveCfg.Masters[bindingId]);

                //重新生成
                GenerateBinding(ipAndPorts, this.slaveCfg.RecvAndSendTimeout, this.slaveCfg.CallBackThreadCount);
            }
            catch
            {
            }
        }

        #region IDisposable 成员

        public void Dispose()
        {
            if (this.statusHunter != null)
                this.statusHunter.Dispose();

            if (this.dataPool != null)
                this.dataPool.Dispose();

            if (this.dispatcher != null)
                this.dispatcher.Dispose();
        }

        #endregion
    }
}
