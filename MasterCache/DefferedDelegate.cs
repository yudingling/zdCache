﻿using System;
using System.Collections.Generic;
using ZdCache.Common.CDataType;

namespace ZdCache.MasterCache
{
    public delegate void SuccessInMaster(ICacheDataType obj);

    public delegate void FailInMaster(Exception ex);
}
