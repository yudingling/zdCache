按个人理解实现的分布式缓存，也一直在完善，欢迎一起修正，提高^^<br />
特性<br />
     1、支持多master配置，配合业务系统分布部署<br />
     2、提供 set、get、update、delete 4种 key value 数据访问<br />
     3、支持同步及异步两种方式调用：<br />
             master.Get(...)<br />
             master.GetAsync(...)<br />
        针对异步，提供jquery deffered 的逼格去传递回调方法： <br />
             master.GetAsync(...).then(successedA, failedA).then(successedB, failedB).then(failedC)...<br />
     4、可扩展的 master 负载均衡策略、slave 缓存过期策略<br />
     5、效率，可以吗？ 应该可以，对于socket、数据处理、多线程、回调等等，一直在进行发现、改善、重构<br />