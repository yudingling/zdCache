using System;
using ZdCache.PorterBase;
using System.IO.Ports;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net.Sockets;
using ZdCache.Common;
using System.Text;
using ZdCache.PorterBase.Setting;
using ZdCache.Common.ActionModels;
using ZdCache.MasterCache.Caller;
using ZdCache.Common.CacheCommon;
using System.Threading.Tasks;

namespace ZdCache.MasterCache
{
    public sealed class Binding : BasePorter
    {
        /// <summary>
        /// 存储所有有效的 slaveModel
        /// </summary>
        private ConcurrentDictionary<int, SlaveModel> slaves = new ConcurrentDictionary<int, SlaveModel>();
        //存储 slave 的 registerID 与 tokenID 的对应关系
        private ConcurrentDictionary<Guid, int> slaveRegisterDic = new ConcurrentDictionary<Guid, int>();

        private PackageDataContainer packageContainer;
        private AsyncCall statusCheckCall;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Binding(BaseSettings setting, ErrorTracer tracer)
            : base(setting, tracer)
        {
            try
            {
                this.packageContainer = new PackageDataContainer();

                this.statusCheckCall = new AsyncCall(new AsyncMethod(StatusCheckCall), null, true, null);
            }
            catch
            {
                //存在有限资源的分配，如果异常，需要释放资源
                this.Close();
                throw;
            }
        }

        /// <summary>
        /// 对slave 状态进行检查，如果超过时间未上报，则认为此 slave 已断开连接，需要删除
        /// </summary>
        /// <param name="args"></param>
        private void StatusCheckCall(AsyncArgs args)
        {
            while (true)
            {
                try
                {
                    Parallel.ForEach<int>(this.slaves.Keys, tokenId =>
                        {
                            SlaveModel tempModel;
                            if (this.slaves.TryGetValue(tokenId, out tempModel))
                            {
                                if (tempModel.Status != null
                                    && (DateTime.Now - tempModel.Status.ReportTM).TotalMilliseconds > ConstParams.MaxIntervalOfStatusWhenSlaveErrorOccured)
                                {
                                    this.slaves.TryRemove(tokenId, out tempModel);
                                }
                            }
                        });
                }
                catch
                {
                }

                //睡眠 MaxIntervalOfStatusWhenSlaveErrorOccured 时长
                SleepHelper.Sleep(ConstParams.MaxIntervalOfStatusWhenSlaveErrorOccured);
            }
        }

        /// <summary>
        /// 数据接收方法。 
        /// </summary>
        /// <param name="tokenID">客户端 id。 对于 comm 来说，默认 0; 对 socket 来说，标记为客户端socket</param>
        /// <param name="data">接收到的数据</param>
        protected override void DataReceived(int tokenID, List<byte[]> allBytes)
        {
            CallArgsModel args = DataArrangement.GetCallObject(this.packageContainer, allBytes, true);
            if (args != null)
            {
                switch (args.AcKind)
                {
                    case ActionKind.APStatusInfo:
                        {
                            StatusInfo status = args.AcArgs.RealObj as StatusInfo;
                            //自报状态信息。如果该 slave 没找到，则 addSlave
                            SlaveModel slave = GetSlave(tokenID, status.SlaveRegisterID);
                            if (slave != null)
                                slave.Status = args.AcArgs.RealObj as StatusInfo;
                            break;
                        }

                    default:
                        {
                            //slave 的回复
                            SlaveModel slave = GetSlave(tokenID);
                            if (slave != null)
                                slave.Receive(args);
                            break;
                        }
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Close()
        {
            if (this.packageContainer != null)
                this.packageContainer.Dispose();

            if (this.statusCheckCall != null)
                this.statusCheckCall.Stop();

            base.Close();
        }

        #region slave add/remove/get

        /// <summary>
        /// 获取当前有效的 slaveModel 列表的一个快照
        /// </summary>
        public ICollection<SlaveModel> Slaves { get { return this.slaves.Values; } }

        /// <summary>
        /// 获取 slaveModel，如果未注册，则注册
        /// </summary>
        private SlaveModel GetSlave(int tokenID, Guid registerID)
        {
            SlaveModel tempModel;

            //注意，此处判断之前不要写成 tryRemove 再 tryAdd 的方式，效率低下
            int oldID;
            if (this.slaveRegisterDic.TryGetValue(registerID, out oldID))
            {
                //如果tokenID 不一致(则说明 slave 重新连接过)
                if (oldID != tokenID)
                {
                    AddNewRegisterID(registerID, registerID, tokenID);
                    AddNewTokenID(oldID, tokenID);
                }
            }
            else
            {
                //添加新的 id
                AddNewRegisterID(Guid.Empty, registerID, tokenID);
                AddNewTokenID(-1, tokenID);
            }

            //注意，此处的返回必须再从 tryGet 一次，因为不能保证上面的所有操作的一致性
            if (this.slaves.TryGetValue(tokenID, out tempModel))
                return tempModel;
            else
                return null;
        }

        private void AddNewTokenID(int oldTokenID, int newTokenID)
        {
            SlaveModel temp;
            if (oldTokenID > 0)
                this.slaves.TryRemove(oldTokenID, out temp);

            this.slaves.TryAdd(newTokenID, new SlaveModel(newTokenID, this));
        }

        private void AddNewRegisterID(Guid oldRegID, Guid newRegID, int newTokenID)
        {
            int temp;
            if (oldRegID != Guid.Empty)
                this.slaveRegisterDic.TryRemove(oldRegID, out temp);

            this.slaveRegisterDic.TryAdd(newRegID, newTokenID);
        }


        public SlaveModel GetSlave(int tokenID)
        {
            SlaveModel temp;
            this.slaves.TryGetValue(tokenID, out temp);
            return temp;
        }

        public bool ContainsSlave(int tokenID)
        {
            return this.slaves.ContainsKey(tokenID);
        }

        #endregion
    }
}
