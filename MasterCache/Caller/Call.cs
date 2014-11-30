using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common.CDataType;
using ZdCache.Common.ActionModels;
using System.Diagnostics;
using System.Threading;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace ZdCache.MasterCache.Caller
{
    /// <summary>
    /// call 基类
    /// </summary>
    public abstract class Call
    {
        /// <summary>
        /// call示例是否能够进行 process。 对于一个 call 来说，其 process 只能被执行一次。
        /// 
        ///    首先你可能想复用，为什么不呢，能减少new 这一开销，而且你可能想到，我在所有的回调结束后，也即 DoFinishProcess 中
        ///    设置其可用性为 true，以便复用，但实际操作有如下考虑： 
        ///             1、process 为同步方式时，没有什么问题，能进行复用
        ///             2、process 为异步方式时，你不能确保 process 方法返回时机与 DoFinishProcess 返回时机的一致性，
        ///                就导致你无法确定在何时设置 processEnabled，以便实例的复用。 比如，你在 DoFinishProcess 最后设置其复用
        ///                状态，但因为线程的顺序不确定性，有可能状态已经设置为可复用了，但 process 还没返回（DoAfterProcess后还有很多相关的逻辑），
        ///                这种情况下，你再调用 process 就会得到响应，而这种响应会更改上一次调用的相关状态，所以是错误的。
        /// 
        /// 基于以上原因，为保证代码的可读性与类耦合，禁止call 实例的复用。
        ///    (因为上面的第二点问题实际应该也是可以解决的，即在 Process 方法
        ///     的最后进行复用性状态的设置，保证call实例本身的状态都已完成，不受影响，同时保证return结果不对自定义的callback产生影响，
        ///     但这影响了代码的可读性与原本设计意图)，
        ///     
        /// </summary>
        private bool processEnabled = true;

        /// <summary>
        /// 标志每个Call，唯一
        /// </summary>
        protected Guid callID = Guid.NewGuid();

        private WaitingContext waitContext;

        /// <summary>
        /// 存储此 call 对应的执行命令的 SlaveModel
        /// </summary>
        protected ConcurrentDictionary<int, SlaveModel> processedList = new ConcurrentDictionary<int, SlaveModel>();

        /// <summary>
        /// call 的总数。 
        /// </summary>
        protected int allCallCount = -1;

        /// <summary>
        /// 标识是否同步
        /// </summary>
        private bool isSync = true;

        /// <summary>
        /// 存储call 执行完成后的自定义委托
        /// </summary>
        protected FinishedDelegate finishCallBack;

        private int doFinishProcessCallCount = 0;

        /// <summary>
        /// 构造函数，默认为同步调用
        /// </summary>
        public Call()
        {
            this.waitContext = new WaitingContext(this.isSync);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public Call(bool sync, FinishedDelegate finished)
        {
            this.isSync = sync;
            this.finishCallBack = finished;

            this.waitContext = new WaitingContext(this.isSync);
        }

        /// <summary>
        /// 处理 call
        /// </summary>
        /// <param name="slaveList"></param>
        /// <param name="args"></param>
        /// <param name="retList">返回的结果，如果操作无返回cachedata，则为 null (比如find 是有结果， set、update、delete 是没有的)</param>
        /// <returns></returns>
        public virtual bool Process(ICollection<SlaveModel> slaveList, ICacheDataType args, out IList<ICacheDataType> retList)
        {
            retList = null;
            return false;
        }

        /// <summary>
        /// 处理 call，异步
        /// </summary>
        /// <param name="slaveList"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public virtual bool Process(ICollection<SlaveModel> slaveList, ICacheDataType args)
        {
            return false;
        }

        /// <summary>
        /// DoBeforeProcess
        /// </summary>
        protected void DoBeforeProcess()
        {
            this.processEnabled = false;

            this.allCallCount = -1;
        }

        /// <summary>
        /// DoAfterProcess
        /// </summary>
        /// <returns>返回是否超时</returns>
        protected bool DoAfterProcess(int callCount, int millisecondsTimeout)
        {
            if (this.waitContext.StartWaiting(callCount, millisecondsTimeout))
                return false;
            else
            {
                //如果超时，则直接调用 DoFinishProcess，否则交由 DoReturnProcess 进行判断是否调用 DoFinishProcess
                return this.DoFinishProcess();
            }
        }

        /// <summary>
        /// DoReturnProcess
        /// </summary>
        protected void DoReturnProcess()
        {
            if (this.waitContext.CallBackHappend())
                this.DoFinishProcess();
        }

        /// <summary>
        /// DoFinishProcess, 此方法 private，内部调用
        /// </summary>
        private bool DoFinishProcess()
        {
            //保证中被执行一次，因为 DoFinishProcess 有可能被多次调用（情形：waitContext.StartWaiting 刚好超时，同时 waitContext.CallBackHappend 返回了 true）。
            //如果成功执行到了则返回 true
            if (Interlocked.Increment(ref this.doFinishProcessCallCount) == 1)
            {
                if (this.processedList.Count > 0)
                {
                    //清除所有的 call
                    Parallel.ForEach(processedList.Values, model =>
                    {
                        model.RemoveCall(this.callID);
                    });

                    //清除 processedList
                    this.processedList.Clear();
                }

                //释放 waitContext
                this.waitContext.Dispose();

                //如果是异步，则调用 CallFinished
                if (!this.isSync)
                    this.CallFinished();

                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// call执行完毕事件
        /// </summary>
        protected virtual void CallFinished() { }

        /// <summary>
        /// 判断在 Processing 过程中是否退出(默认返回 false)。
        /// 比如 get 操作且返回唯一值，则当收到一个结果后，即可停止剩余的所有 process
        /// </summary>
        public virtual bool BreakWhenProcessing { get { return false; } }

        /// <summary>
        /// call 的唯一标识
        /// </summary>
        public Guid ID { get { return this.callID; } }

        /// <summary>
        /// 是否能进行 process
        /// </summary>
        protected bool ProcessEnabled { get { return this.processEnabled; } }
    }
}
