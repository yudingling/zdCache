using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using ZdCache.MasterCache.Caller;
using System.Threading;

namespace ZdCache.MasterCache
{
    public class CallProcessor
    {
        /// <summary>
        /// 执行Action (get\update\delete)
        /// </summary>
        /// <returns>call 的数量（此处不能用 processedList.Count 去表示所有的数量，因为 processedList 会在回调中被清除）</returns>
        public static int Process(ICollection<SlaveModel> slaveList, ConcurrentDictionary<int, SlaveModel> processedList, MasterCallArgsModel callArgModel, Call masterCall)
        {
            int allCallCount = 0;
            processedList.Clear();

            try
            {
                //并行执行
                Parallel.ForEach(slaveList, (model, state) =>
                {
                    //如果 BreakWhenProcessing，则需要退出整个 Parallel
                    if (masterCall.BreakWhenProcessing)
                    {
                        state.Stop();
                        return;
                    }

                    //注意，必须先加到 processedList，再 call，避免因为 call 已经产生了回调，但 processedList 此时还没有加入的问题
                    if (processedList.TryAdd(model.ID, model))
                    {
                        Interlocked.Increment(ref allCallCount);

                        //对于服务端的调用而言，不管是 socket 异常，还是其他的因素导致的异常，都不应该反馈到外部调用者知道，外部调用者只需要
                        //知道是否成功，所以此处不抛出异常
                        try
                        {
                            model.Call(callArgModel);
                        }
                        catch
                        {
                            //出现异常则删除此 process
                            processedList.TryRemove(model.ID, out model);
                        }
                    }
                });
            }
            catch
            {
            }

            return allCallCount;
        }

        /// <summary>
        /// 执行 Action (set);
        /// 对一个执行 set，则其他的都要执行 delete，避免重复设置
        /// </summary>
        public static int Process(ICollection<SlaveModel> slaveList, SlaveModel slaveForSet, ConcurrentDictionary<int, SlaveModel> processedList,
            MasterCallArgsModel callArgModelForSet, MasterCallArgsModel callArgModelForDel, Call masterCall)
        {
            int allCallCount = 0;
            processedList.Clear();

            try
            {
                Parallel.ForEach(slaveList, (model, state) =>
                {
                    if (masterCall.BreakWhenProcessing)
                    {
                        state.Stop();
                        return;
                    }

                    if (processedList.TryAdd(model.ID, model))
                    {
                        Interlocked.Increment(ref allCallCount);

                        try
                        {
                            if (model.ID == slaveForSet.ID)
                                model.Call(callArgModelForSet);
                            else
                                model.Call(callArgModelForDel);
                        }
                        catch
                        {
                            //出现异常则删除此 process
                            processedList.TryRemove(model.ID, out model);
                        }
                    }
                });
            }
            catch
            {
            }

            return allCallCount;
        }

    }
}
