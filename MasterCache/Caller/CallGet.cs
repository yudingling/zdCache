using System;
using System.Collections.Concurrent;
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

        #region 构造函数

        public CallGet()
            : base()
        {
        }

        public CallGet(bool sync, FinishedDelegate finished)
            : base(sync, finished)
        {
        }

        public CallGet(bool sync, bool retUnique, FinishedDelegate finished)
            : base(sync, finished)
        {
            this.isRetUnique = retUnique;
        }

        #endregion

        /// <summary>
        /// 执行查找。 阻塞
        /// </summary>
        public override bool Process(ICollection<SlaveModel> slaveList, ICacheDataType args, out IList<ICacheDataType> retList)
        {
            if (!this.ProcessEnabled || slaveList == null || slaveList.Count == 0)
            {
                retList = null;
                return false;
            }

            this.DoBeforeProcess();
            this.allCallCount = CallProcessor.Process(slaveList, this.processedList, new MasterCallArgsModel(this.callID, ActionKind.Get, args, FindCallReturn), this);
            if (this.DoAfterProcess(this.allCallCount, ConstParams.CallTimeOut))
                throw new Exception("find timeout!");

            retList = this.allCallRets;

            return this.allCallRets.Count > 0;
        }

        /// <summary>
        /// 执行查找。非阻塞
        /// </summary>
        public override bool Process(ICollection<SlaveModel> slaveList, ICacheDataType args)
        {
            if (!this.ProcessEnabled || slaveList == null || slaveList.Count == 0)
                return false;

            this.DoBeforeProcess();
            this.allCallCount = CallProcessor.Process(slaveList, this.processedList, new MasterCallArgsModel(this.callID, ActionKind.Get, args, FindCallReturn), this);
            this.DoAfterProcess(this.allCallCount, 0);

            //存在执行的 call 则返回 true
            return this.allCallCount > 0;
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

        protected override void CallFinished()
        {
            if (this.finishCallBack != null)
            {
                if (this.allCallRets.Count > 0)
                    this.finishCallBack(this.ID, true, this.allCallRets);
                else
                    this.finishCallBack(this.ID, false, new Exception("获取数据失败！"));
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
