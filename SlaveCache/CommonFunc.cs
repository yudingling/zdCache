using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZdCache.Common;
using ZdCache.Common.CacheCommon;

namespace ZdCache.SlaveCache
{
    internal class CommonFunc
    {
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="binding"></param>
        /// <param name="model"></param>
        public static void SendCall(ClientBinding binding, CallArgsModel model)
        {
            List<byte[]> dataList = DataArrangement.GetCallBytes(model);

            foreach (byte[] data in dataList)
            {
                if (data.Length > 0)
                    binding.Send(data);
            }
        }
    }
}
