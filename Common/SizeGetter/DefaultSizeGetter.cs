using System;

namespace ZdCache.Common.SizeGetter
{
    /// <summary>
    /// 默认的大小获取器(此处实现 zdCache 的 sizeGetter)。 直接返回 SizeGetResult.NoSizeFlag
    /// </summary>
    public class DefaultSizeGetter : ISizeGetter
    {
        public virtual SizeGetResult GetSize(byte[] sourceData, int dataLength, out int realStartIndex, out int realSize)
        {
            realStartIndex = -1;
            realSize = -1;

            //根据 DataArrangement 中的约定，前4个字节为长度
            if (dataLength < 4)
                return SizeGetResult.ParaBytesNotEnough;

            realStartIndex = 0;
            realSize = Function.GetInt32FromBytes(sourceData, realStartIndex);

            return SizeGetResult.Success;
        }
    }
}
