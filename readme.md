分布式缓存练手<br />
### 特性<br />
     1、多master配置，配合业务系统分布部署

     2、 set、get、update、delete 4种 key value 数据访问

     3、同步及异步两种方式调用：
             master.Get(...)
             master.GetAsync(...)

        针对异步，提供jquery deffered 方式传递回调方法：
             master.GetAsync(...).then(successedA, failedA).then(failedB)...

     4、可扩展的 master 负载均衡策略、slave 缓存过期策略
