using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.PorterBase
{
    /// <summary>
    /// 用于存储从 scoket 读取的字节数组， 此 DataContainer 中的数据对应一次完整的数据接收
    /// </summary>
    internal class DataContainer
    {
        private int bytesLength = 0;
        private List<byte[]> bytesList;

        internal DataContainer()
        {
            this.bytesList = new List<byte[]>();
        }

        /// <summary>
        /// 将某一次socket 接收到数据 copy 到 DataContainer 中，注意，Assign 的顺序，业务中对数据的的合成将按加入的顺序来
        /// </summary>
        /// <param name="source"></param>
        /// <param name="startIndex"></param>
        /// <param name="length"></param>
        internal void Assign(byte[] source, int startIndex, int length)
        {
            if (source != null && startIndex >= 0 && length > 0 && startIndex + length <= source.Length)
            {
                byte[] itemAdd = new byte[length];
                Array.Copy(source, startIndex, itemAdd, 0, length);
                this.bytesLength += length;
                bytesList.Add(itemAdd);
            }
        }

        /// <summary>
        /// 重置bytesList，是因为某一次回调完成后，其数据就废弃了，需要手动清除以便回收。 注意，此处不能用 Clear 去清除，因为在回调的使用用到了此 bytesList，如果清除了，那就影响了回调
        /// </summary>
        internal void Reset()
        {
            this.bytesLength = 0;
            this.bytesList = new List<byte[]>();
        }

        /// <summary>
        /// 获取数据内容
        /// </summary>
        internal List<byte[]> BytesList
        {
            get { return this.bytesList; }
        }

        /// <summary>
        /// 字节的总数
        /// </summary>
        internal int Length
        {
            get { return this.bytesLength; }
        }
    }
}
