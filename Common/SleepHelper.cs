using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;

namespace ZdCache.Common
{
    /// <summary>
    /// 通过 ManualResetEvent ，结合 Async，完美结束外部线程
    /// 使用方式： 
    ///     SleepHelper.Sleep(millseconds);
    ///     对于可自行结束的方法，不是 while(true){....; SleepHelper.Sleep(1000);} 那样方式，请在 Sleep 方法结束后，执行： SleepHelper.RemoveResetEvent();
    /// </summary>
    public class SleepHelper
    {
        /// <summary>
        /// 注意，此处必须要使用线程安全字典, 4.0 就是好，自带，哈哈
        /// </summary>
        public static ConcurrentDictionary<Thread, ManualResetEvent> AllResetEvent = new ConcurrentDictionary<Thread, ManualResetEvent>();

        static SleepHelper()
        {
            AsyncCall call = new AsyncCall(new AsyncMethod(ClearStopThread), null, true, null);
        }

        /// <summary>
        /// 定时清理无效线程
        /// </summary>
        /// <param name="arg"></param>
        private static void ClearStopThread(AsyncArgs arg)
        {
            ManualResetEvent outValue;
            ManualResetEvent resetEventS = new ManualResetEvent(false);
            while (true)
            {
                foreach (Thread thread in AllResetEvent.Keys)
                {
                    if (!thread.IsAlive)
                        AllResetEvent.TryRemove(thread, out outValue);
                }
                //
                resetEventS.Reset();
                resetEventS.WaitOne(60000);
            }
        }

        #region get

        /// <summary>
        /// 获取当前线程的 ManualResetEvent，每个线程有且只能有一个 ManualResetEvent
        /// </summary>
        /// <returns></returns>
        private static ManualResetEvent GetResetEvent()
        {
            ManualResetEvent resetEvent = null;
            if (!AllResetEvent.TryGetValue(Thread.CurrentThread, out resetEvent))
            {
                ManualResetEvent manualReset = new ManualResetEvent(false);
                ////注意此处的写法，不能直接 return manualReset，因为此处新建的 manualReset 有可能没有加到 AllResetEvent （多线程缘故），
                ////要保持此 GetResetEvent 方法返回的对象都处于 AllResetEvent 中
                //if (AllResetEvent.TryAdd(Thread.CurrentThread, manualReset))
                //    return manualReset;
                //else
                //    return AllResetEvent[Thread.CurrentThread];

                //上面的顾虑是没必要的，因为对于某一个线程来说（Thread.CurrentThread），对 GetResetEvent 方法的调用始终是单线程的，
                //所以对 AllResetEvent 的追加都会成功，不会失败 
                AllResetEvent.TryAdd(Thread.CurrentThread, manualReset);
                return manualReset;
            }
            else
                return resetEvent;
        }

        /// <summary>
        /// 获取指定线程对应的 ManualResetEvent
        /// </summary>
        /// <param name="thread"></param>
        /// <returns></returns>
        private static ManualResetEvent GetResetEvent(Thread thread)
        {
            ManualResetEvent resetEvent = null;
            if (AllResetEvent.TryGetValue(thread, out resetEvent))
                return resetEvent;
            else
                return null;
        }

        #endregion

        #region remove

        /// <summary>
        /// 清除当前线程对应的 ManualResetEvent。
        /// 使用时注意：对于可自行结束的方法，不是 while(true) 那样的，Sleep 方法结束后，应该执行 RemoveResetEvent，避免 AllResetEvent 中引用太多无效的 ManualResetEvent
        /// </summary>
        public static void RemoveResetEvent()
        {
            ManualResetEvent outValue;
            AllResetEvent.TryRemove(Thread.CurrentThread, out outValue);
        }

        /// <summary>
        /// 清除thread 对应的 ManualResetEvent
        /// </summary>
        /// <param name="thread"></param>
        public static void RemoveResetEvent(Thread thread)
        {
            ManualResetEvent outValue;
            AllResetEvent.TryRemove(thread, out outValue);
        }

        #endregion

        #region SetSingle

        /// <summary>
        /// 给当前线程对应的 ManualResetEvent 信号，唤醒线程继续执行
        /// </summary>
        public static void SetSignal()
        {
            ManualResetEvent resetEvent = GetResetEvent(Thread.CurrentThread);
            if (resetEvent != null)
                resetEvent.Set();
        }

        /// <summary>
        /// 给 thread 线程对应的 ManualResetEvent 信号，唤醒线程继续执行
        /// </summary>
        public static void SetSignal(Thread thread)
        {
            ManualResetEvent resetEvent = GetResetEvent(thread);
            if (resetEvent != null)
                resetEvent.Set();
        }

        #endregion

        #region sleep

        public static void Sleep(int millSeconds)
        {
            ManualResetEvent resetEvent = GetResetEvent();
            resetEvent.Reset();
            resetEvent.WaitOne(millSeconds);
        }

        #endregion
    }
}
