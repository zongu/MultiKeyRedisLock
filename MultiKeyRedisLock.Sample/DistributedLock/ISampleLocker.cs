
namespace MultiKeyRedisLock.Sample.DistributedLock
{
    using System.Collections.Generic;

    /// <summary>
    /// mongo分佈式鎖介面範例
    /// </summary>
    public interface ISampleLocker
    {
        /// <summary>
        /// 加鎖
        /// </summary>
        /// <returns></returns>
        IRedisLck GrabLock(IEnumerable<string> keys);
    }
}
