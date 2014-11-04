using System;
using System.Collections.Concurrent;
using ZdCache.Common.ActionModels;
using System.Collections.Generic;

namespace ZdCache.Common.CacheCommon
{
    /// <summary>
    /// 用于描述被拆分的包
    /// </summary>
    public class PackageDataArg
    {
        //唯一标志，同一 CallID 的 PackageDataArg 属于同一组，用于合并对象
        private Guid callID;
        private int packageCount;

        private ActionKind _acKind;
        private ActionResult _acResult;
        private string _dataTypeName;
        private byte _category;
        private string _argID;

        private ConcurrentDictionary<int, List<byte[]>> packageData = new ConcurrentDictionary<int, List<byte[]>>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public PackageDataArg(Guid id, int packetTotalCount)
        {
            this.callID = id;
            this.packageCount = packetTotalCount;

            this.UpdateTM = DateTime.Now;
        }

        /// <summary>
        /// 添加被拆分的包数据(头部信息)；
        /// 注意方法的实现，必须在 packageData.TryAdd 之前进行ActionKind\ActionResult 等头部信息的更新。
        /// </summary>
        public void AddPackage(int order, List<byte[]> data,
            ActionKind acKind, ActionResult acResult, string dataTypeName, byte category, string argID)
        {
            this._acKind = acKind;
            this._acResult = acResult;
            this._dataTypeName = dataTypeName;
            this._category = category;
            this._argID = argID;
            this.UpdateTM = DateTime.Now;

            //此语句必须在上面的头部信息更新之后执行， 因为判断是否接受完成的标识是
            this.packageData.TryAdd(order, data);
        }

        /// <summary>
        /// 添加被拆分的包数据(argObj 实体)
        /// </summary>
        public void AddPackage(int order, List<byte[]> data)
        {
            this.UpdateTM = DateTime.Now;

            this.packageData.TryAdd(order, data);
        }

        /// <summary>
        /// 获取数据包的 bute[] 列表
        /// </summary>
        /// <param name="order">包序号</param>
        /// <returns></returns>
        public List<byte[]> GetPackage(int order)
        {
            List<byte[]> temp;
            this.packageData.TryGetValue(order, out temp);
            return temp;
        }

        public ActionKind AcKind { get { return this._acKind; } }
        public ActionResult AcResult { get { return this._acResult; } }
        public string DataTypeName { get { return this._dataTypeName; } }
        public byte Category { get { return this._category; } }
        public string ArgID { get { return this._argID; } }

        /// <summary>
        /// callID 唯一标识
        /// </summary>
        public Guid CallID { get { return this.callID; } }

        /// <summary>
        /// 数据包总数
        /// </summary>
        public int PackageCount { get { return this.packageCount; } }

        /// <summary>
        /// 是否所有包都接收完成
        /// </summary>
        public bool IsAllPackectBuilt { get { return this.packageData.Count >= this.packageCount; } }

        /// <summary>
        /// 更新时间，用于后续的定时清除（用于清除出现异常无法全部接收的数据）
        /// </summary>
        public DateTime UpdateTM { get; set; }
    }
}
