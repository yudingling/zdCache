using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using ZdCache.Common;
using System.Threading.Tasks;
using ZdCache.MasterCache.LoadbalanceStrategy;

namespace ZdCache.MasterCache
{
    public class BalanceHandler : IDisposable
    {
        private SlaveModel[] sortedModeList;

        private DateTime preSortCmdTM = DateTime.Parse("1970-12-01");

        private Binding binding;
        private ILoadBalanceStrategy balanceStrategy;
        private AsyncCall call;

        //排序命令池
        private ConcurrentStack<byte> cmds = new ConcurrentStack<byte>();

        /// <summary>
        /// 狗仔函数
        /// </summary>
        /// <param name="myBinding"></param>
        /// <param name="strategy">负载平衡策略</param>
        public BalanceHandler(Binding myBinding, ILoadBalanceStrategy strategy)
        {
            this.binding = myBinding;
            this.balanceStrategy = strategy;

            this.call = new AsyncCall(new AsyncMethod(DoneSort), null, true, null);
        }

        /// <summary>
        /// 提交排序命令，如果与上一次提交的命令时间在100ms以内，则忽略此条命令
        /// 状态信息是1s上报一次，为什么提高此处的频率有用呢？ 是因为 master 端在进行set缓存操作的时候，提前进行了粗略的 slave 信息更新(缓存增减的大小)
        /// </summary>
        public void Sort()
        {
            DateTime cur = DateTime.Now;
            if ((cur - preSortCmdTM).TotalMilliseconds >= 100)
            {
                preSortCmdTM = cur;
                cmds.Push(1);
            }
        }

        /// <summary>
        /// 执行排序
        /// </summary>
        /// <param name="args"></param>
        private void DoneSort(AsyncArgs args)
        {
            while (true)
            {
                try
                {
                    if (this.cmds.Count == 0)
                        this.cmds.Push(1);

                    while (this.cmds.Count > 0)
                    {
                        this.cmds.Clear();

                        this.sortedModeList = this.balanceStrategy.MakeBalance(this.binding.Slaves);

                        //如果 sortCmds 存在更多的记录，则100ms 执行一次
                        SleepHelper.Sleep(100);
                    }
                }
                catch
                {
                }

                //无 Sort 干扰的情况下，1s 执行一次排序(因为状态信息的上报是 1s 一次)
                SleepHelper.Sleep(1000);
            }
        }

        /// <summary>
        /// 负载最低的 SlaveModel
        /// </summary>
        /// <param name="cacheSize">缓存的大小(字节)</param>
        /// <param name="cacheSize">相对的起始位置</param>
        /// <returns></returns>
        public SlaveModel GetMostWantedSlave(long cacheSize, ref int startIndex)
        {
            //转换为 kb
            long tempSize = cacheSize / 1024;

            if (this.sortedModeList == null)
                this.sortedModeList = this.balanceStrategy.MakeBalance(this.binding.Slaves);

            if (this.sortedModeList != null)
            {
                SlaveModel tempModel = null;
                for (; startIndex < this.sortedModeList.Length; startIndex++)
                {
                    tempModel = this.sortedModeList[startIndex];
                    //注意，这里还需要判断是 tempModel 在 binding 中是否存在（因为 binding 中有对 status 进行检测并删除）
                    //其实无论怎样，都应该对 tempModel 进行是否存在的判断，因为 GetMostWantedSlave 方法的调用不可能与 slave 的状态维护动作保持一致。
                    if (tempModel.Status.FreeMem > tempSize && this.binding.ContainsSlave(tempModel.ID))
                    {
                        //注意，这里需要给 startIndex 加1，因为下面 return 了
                        startIndex++;
                        return tempModel;
                    }
                }
            }

            return null;
        }

        #region IDisposable 成员

        public void Dispose()
        {
            if (this.call != null)
                this.call.Stop();
        }

        #endregion
    }
}
