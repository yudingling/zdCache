using System;
using System.Collections.Concurrent;
using System.Threading;
using ZdCache.Common;
using ZdCache.Common.ActionModels;
using ZdCache.Common.CDataType;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;
using ZdCache.Common.CacheCommon;

namespace ZdCache.MasterCache.Caller
{
    /// <summary>
    /// Get call
    /// </summary>
    public class CallGet : Call
    {
        //表示结果是否唯一，如果唯一，则表示操作只有在一个Slave中有结果
        protected bool isRetUnique = false;

        //存储所有 slave 的返回值
        private List<ICacheDataType> allCallRets = new List<ICacheDataType>();

        public CallGet()
            : base()
        {
        }

        public CallGet(bool retUnique)
            : base()
        {
            this.isRetUnique = retUnique;
        }

        /// <summary>
        /// 执行查找。 Process 方法对外是阻塞调用的
        /// </summary>
        public override bool Process(ICollection<SlaveModel> slaveList, ICacheDataType args, out List<ICacheDataType> retList)
        {
            if (slaveList == null || slaveList.Count == 0)
            {
                retList = null;
                return false;
            }

            this.DoBeforeProcess();
            this.allCallCount = CallProcessor.Process(slaveList, this.processedList, new MasterCallArgsModel(this.callID, ActionKind.Get, args, FindCallReturn), this);
            bool isTimeOut = this.DoAfterProcess(ConstParams.CallTimeOut);

            //结束 action
            Stop();

            //action 超时，则抛出异常
            if (isTimeOut)
                throw new Exception("find timeout!");

            retList = this.allCallRets;

            return this.allCallRets.Count > 0;
        }

        private void FindCallReturn(ReturnArgsModel returnArgsModel)
        {
            SlaveModel tempModel;
            if (this.processedList.TryRemove(returnArgsModel.Slave.ID, out tempModel))
            {
                if (returnArgsModel.AcResult == ActionResult.Succeed)
                {
                    //必须在设置 DoReturnProcess 语句之前， add 结果到 allCallRets
                    this.allCallRets.Add(returnArgsModel.Data);
                }

                this.DoReturnProcess();
            }
        }

        /// <summary>
        /// 如果为 isRetUnique 且获取到了资料，则需要 break process
        /// </summary>
        public override bool BreakWhenProcessing
        {
            get
            {
                return this.isRetUnique && this.allCallRets.Count > 0;
            }
        }
    }
}
