using MultiKeyRedisLock.Sample.Applibs;
using MultiKeyRedisLock.Sample.DistributedLock;

try
{
    ISampleLocker locker = new SampleLocker(NoSqlService.DistributedLockService);

    using (var lck = locker.GrabLock(new[] { "TEST001" }))
    {
        // 取不到鎖
        if (!lck.IsAcquired)
        {
            throw new Exception($"Can Not Acquire RedisLockSample, UnAcquareLockIds:{string.Join(",", lck.UnAcquareLockIds)}");
        }

        // 取到鎖之後開始處理需要備份布式鎖保護資源運用
        // ..............
    }
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
}

Console.Read();