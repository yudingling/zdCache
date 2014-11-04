using System;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;

namespace ZdCache.Common
{
    /// <summary>
    /// 线程池
    /// </summary>
    public class SThreadPool
    {
        //task 的增长ID
        private static int increamentID = 0;
        //初始线程池的大小
        private static int initialCapacity = 10;
        //此处的 Stack 必须是线程安全的
        private static ConcurrentStack<SThreadTask> myThreadPool;

        static SThreadPool()
        {
            myThreadPool = new ConcurrentStack<SThreadTask>();
            //创建初始的 Task 容量
            for (int i = 0; i < initialCapacity; i++)
            {
                myThreadPool.Push(new SThreadTask(GetNewTaskID()));
            }
        }

        /// <summary>
        /// 从线程池中获取 TreadTask， 如果池中不够，则返回一个新建的 ThreadTask 
        /// </summary>
        /// <returns></returns>
        public static SThreadTask Pop()
        {
            SThreadTask ttTMP;
            if (myThreadPool.TryPop(out ttTMP))
                return ttTMP;
            else
                return new SThreadTask(GetNewTaskID());
        }

        /// <summary>
        /// 将 SThreadTask 对象放回线程池中 
        /// </summary>
        /// <param name="task"></param>
        public static void Push(SThreadTask task)
        {
            myThreadPool.Push(task);
        }

        /// <summary>
        /// 新建 task 的唯一标志
        /// </summary>
        /// <returns></returns>
        private static int GetNewTaskID()
        {
            return Interlocked.Increment(ref increamentID);
        }
    }
}
