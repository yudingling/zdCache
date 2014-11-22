using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using ZdCache.Common.CDataType;
using ZdCache.Common.ActionModels;
using ZdCache.Common;
using ZdCache.Common.CacheCommon;

namespace ZdCache.MasterCache.Caller
{
    /// <summary>
    /// delete call
    /// </summary>
    public class CallDelete : Call
    {
        protected bool isSuccess = false;

        #region 构造函数

        public CallDelete() : base() { }
        public CallDelete(bool sync, FinishedDelegate finished) : base(sync, finished) { }

        #endregion

        public override bool Process(ICollection<SlaveModel> slaveList, ICacheDataType args, out IList<ICacheDataType> retList)
        {
            retList = null;
            if (slaveList == null || slaveList.Count == 0)
                return false;

            this.DoBeforeProcess();
            this.allCallCount = CallProcessor.Process(slaveList, this.processedList, new MasterCallArgsModel(this.callID, ActionKind.Delete, args, DeleteCallReturn), this);
            if (this.DoAfterProcess(ConstParams.CallTimeOut))
                throw new Exception("delete timeout!");

            return this.isSuccess;
        }

        private void DeleteCallReturn(ReturnArgsModel returnArgsModel)
        {
            SlaveModel tempModel;
            if (this.processedList.TryRemove(returnArgsModel.Slave.ID, out tempModel))
            {
                //只要有一个delete成功，则设置成功
                //设置是否成功， 注意，必须在 DoReturnProcess 前设置，保证阻塞（DoAfterProcess）退出前 isSuccess 已更新
                if (returnArgsModel.AcResult == ActionResult.Succeed)
                    this.isSuccess = true;

                this.DoReturnProcess();
            }
        }

    }
}
