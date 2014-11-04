using System;

namespace ZdCache.Common.SizeGetter
{
    /// <summary>
    /// 长度获取器的接口方法
    /// </summary>
    public interface ISizeGetter
    {
        /// <summary>
        /// 获取数据包的真实长度； 此实例方法的调用是多线程的
        /// </summary>
        /// <param name="sourceData">原始数据</param>
        /// <param name="dataLength">原始数据的长度， 起始位置从0开始</param>
        /// <param name="realStartIndex">真实数据包在 sourceData 中的起始地址</param>
        /// <param name="realSize">真实长度</param>
        /// <returns>获取结果</returns>
        SizeGetResult GetSize(byte[] sourceData, int dataLength, out int realStartIndex, out int realSize);
    }
}
