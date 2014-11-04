using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.Common
{
    /// <summary>
    /// 简单自定义线程安全List
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MyConcurrentList<T>
    {
        private object lockObj = new object();

        private List<T> list = new List<T>();

        /// <summary>
        /// 追加到末尾
        /// </summary>
        /// <param name="value"></param>
        public void Append(T value)
        {
            lock (lockObj)
            {
                list.Add(value);
            }
        }

        /// <summary>
        /// 从list 中移除第一个，并返回其值
        /// </summary>
        public bool RemoveFirst(out T value)
        {
            value = default(T);
            if (list.Count > 0)
            {
                lock (lockObj)
                {
                    value = list[0];
                    list.RemoveAt(0);
                    return true;
                }
            }
            else
                return false;
        }

        /// <summary>
        /// 是否存在
        /// </summary>
        public bool Contains(T value)
        {
            return list.Contains(value);
        }
    }
}
