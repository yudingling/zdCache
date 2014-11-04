using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZdCache.Common.ActionModels
{
    /// <summary>
    /// 操作类型枚举
    /// </summary>
    public enum ActionKind
    {
        /// <summary>
        /// Slave 自报信息
        /// </summary>
        APStatusInfo,

        /// <summary>
        /// 设置（添加）
        /// </summary>
        Set,

        /// <summary>
        /// 查找
        /// </summary>
        Get,

        /// <summary>
        /// 更新
        /// </summary>
        Update,

        /// <summary>
        /// 删除
        /// </summary>
        Delete
    }
}
