using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using ZdCache.Common.CacheCommon;

namespace ZdCache.Common.CDataType
{
    /// <summary>
    /// bitmap 缓存类型
    /// 注意，所有从 CacheDataType 继承的都要应用此 CacheDataTypeAttribute 属性，不然就不能在缓存时使用此类型，并且 key 要唯一
    /// </summary>
    [CacheDataTypeAttribute(1)]
    public class CacheBitmap : BaseCacheDataType
    {
        private Bitmap bitmap;

        /// <summary>
        /// 构造方法，构造用于查询的 CacheData
        /// </summary>
        /// <param name="identify"></param>
        public CacheBitmap(string identify)
            : base(identify)
        {
        }

        /// <summary>
        /// 构造方法，构造默认 category=0 的 CacheData
        /// </summary>
        /// <param name="identify"></param>
        /// <param name="pic"></param>
        public CacheBitmap(string identify, Bitmap pic)
            : base(identify)
        {
            this.data = new List<byte[]>();
            this.bitmap = pic;
            this.data.Add(GetBmpBytes(pic));
        }

        /// <summary>
        /// 构造方法，构造指定 category 的 CacheData
        /// </summary>
        /// <param name="categoryId"></param>
        /// <param name="identify"></param>
        /// <param name="pic"></param>
        public CacheBitmap(byte categoryId, string identify, Bitmap pic)
            : base(categoryId, identify)
        {
            this.data = new List<byte[]>();
            this.bitmap = pic;
            this.data.Add(GetBmpBytes(pic));
        }

        /// <summary>
        /// 构造方法， 用于从socket 数据构造 CacheData
        /// </summary>
        public CacheBitmap(byte categoryId, string identify, List<byte[]> dataList, bool generateRealObj)
            : base(categoryId, identify, dataList)
        {
            if (generateRealObj)
            {
                int size = 0;
                foreach (byte[] data in dataList)
                    size += data.Length;
                if (size <= 12)
                    throw new Exception("创建bitmap对象失败：长度至少要大于12");

                //这里不考虑申请失败的情况，直接将 dataList 中的数据合并到一个 byte[] 数组，以便生成图像
                byte[] content = new byte[this.totalSize];
                int offSet = 0;
                foreach (byte[] data in dataList)
                {
                    Array.Copy(data, 0, content, offSet, data.Length);
                    offSet += data.Length;
                }

                this.bitmap = new Bitmap(Function.GetInt32FromBytes(content, content.Length - 12), Function.GetInt32FromBytes(content, content.Length - 8));
                BitmapData bdData = this.bitmap.LockBits(new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height), ImageLockMode.ReadOnly,
                    (PixelFormat)Function.GetInt32FromBytes(content, content.Length - 4));
                int Stride = bdData.Stride;
                IntPtr Ptr = bdData.Scan0;
                Marshal.Copy(content, 0, Ptr, content.Length - 12);
                this.bitmap.UnlockBits(bdData);
            }
        }

        public override object RealObj
        {
            get { return this.bitmap; }
        }

        private byte[] GetBmpBytes(Bitmap pic)
        {
            //获取字节流，不要用 memorystream 的方式去获取，那种方式太慢
            BitmapData Data = this.bitmap.LockBits(new Rectangle(0, 0, this.bitmap.Width, this.bitmap.Height), ImageLockMode.ReadOnly, this.bitmap.PixelFormat);
            IntPtr Ptr = Data.Scan0;
            byte[] bmpData = new Byte[this.bitmap.Height * Data.Stride - 1 + 12];
            Marshal.Copy(Ptr, bmpData, 0, bmpData.Length - 12);
            this.bitmap.UnlockBits(Data);

            //最后12个byte 分别表示bitmap的 width, height, PixelFormat
            Array.Copy(Function.GetBytesFormInt32(this.bitmap.Width), 0, bmpData, bmpData.Length - 12, 4);
            Array.Copy(Function.GetBytesFormInt32(this.bitmap.Height), 0, bmpData, bmpData.Length - 8, 4);
            Array.Copy(Function.GetBytesFormInt32((Int32)this.bitmap.PixelFormat), 0, bmpData, bmpData.Length - 4, 4);

            return bmpData;
        }
    }
}
