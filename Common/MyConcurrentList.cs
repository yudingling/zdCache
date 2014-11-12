using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ZdCache.Common
{
    /// <summary>
    /// 简单自定义线程安全List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MyConcurrentList<T>
    {
        private ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();

        private List<T> list = new List<T>();

        /// <summary>
        /// 追加到末尾
        /// </summary>
        /// <param name="value"></param>
        public void Append(T value)
        {
            if (rwLock.TryEnterWriteLock(-1))
            {
                try
                {
                    list.Add(value);
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// 从list 中移除第一个，并返回其值
        /// </summary>
        public bool RemoveFirst(out T value)
        {
            value = default(T);

            if (rwLock.TryEnterWriteLock(-1))
            {
                try
                {
                    if (list.Count > 0)
                    {

                        value = list[0];
                        list.RemoveAt(0);
                        return true;
                    }
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }

            return false;
        }

        /// <summary>
        /// 是否存在
        /// </summary>
        public bool Contains(T value)
        {
            if (rwLock.TryEnterReadLock(-1))
            {
                try
                {
                    return list.Contains(value);
                }
                finally
                {
                    rwLock.ExitReadLock();
                }
            }

            return list.Contains(value);
        }

        public int Count
        {
            get
            {
                if (rwLock.TryEnterReadLock(-1))
                {
                    try
                    {
                        return list.Count;
                    }
                    finally
                    {
                        rwLock.ExitReadLock();
                    }
                }

                return list.Count;
            }
        }
    }
}
