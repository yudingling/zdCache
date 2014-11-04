using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using ZdCache.PorterBase;
using ZdCache.Common;
using ZdCache.MasterCache.Caller;
using ZdCache.Common.CacheCommon;
using System.Collections.Generic;

namespace ZdCache.MasterCache
{
    public class SlaveModel
    {
        private Binding myBinding;

        /// <summary>
        /// 对应到 socket accpet 到的 tokenID
        /// </summary>
        private int tokenID;

        /// <summary>
        /// slave 状态信息
        /// </summary>
        public StatusInfo Status { get; set; }

        /// <summary>
        /// 存储Call 的 标志 -- SlaveCallReturn
        /// </summary>
        ConcurrentDictionary<Guid, SlaveCallReturn> callRetList = new ConcurrentDictionary<Guid, SlaveCallReturn>();

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="socketTokenID"></param>
        /// <param name="serverBind"></param>
        public SlaveModel(int socketTokenID, Binding serverBind)
        {
            this.tokenID = socketTokenID;
            this.myBinding = serverBind;

            //初始一个默认的信息
            this.Status = new StatusInfo();
        }
        
        /// <summary>
        /// SlaveModel 的标识
        /// </summary>
        public int ID { get { return this.tokenID; } }

        public void Call(MasterCallArgsModel callArgModel)
        {
            if (this.callRetList.TryAdd(callArgModel.AcCallID, callArgModel.AcCallReturn))
            {
                List<byte[]> dataList = DataArrangement.GetCallBytes(callArgModel);

                foreach (byte[] data in dataList)
                {
                    if (data.Length > 0)
                        this.myBinding.Send(this.tokenID, data);
                }
            }
        }

        /// <summary>
        /// slave 返回结果回调
        /// </summary>
        /// <param name="retArgModel"></param>
        public void Receive(CallArgsModel retArgModel)
        {
            SlaveCallReturn retFunc;
            if (this.callRetList.TryRemove(retArgModel.AcCallID, out retFunc))
            {
                retFunc(new ReturnArgsModel(this, retArgModel.AcResult, retArgModel.AcArgs));
            }
        }

        /// <summary>
        /// 清除指定 call
        /// </summary>
        /// <param name="retArgModel"></param>
        public void RemoveCall(Guid callID)
        {
            SlaveCallReturn retFunc;
            this.callRetList.TryRemove(callID, out retFunc);
        }
    }
}
