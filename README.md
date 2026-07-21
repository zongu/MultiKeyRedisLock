# Redis分佈式鎖
## 使用範例

```
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

/// <summary>
/// 分佈式鎖範例
/// </summary>
public class SampleLocker : ISampleLocker
{
    /// <summary>
    /// 分佈式鎖工廠
    /// </summary>
    private RedisLockFactory factory;
    public SampleLocker(RedisLockFactory factory)
    {
        this.factory = factory;
    }
    /// <summary>
    /// 加鎖
    /// </summary>
    /// <returns></returns>
    public IRedisLck GrabLock(IEnumerable<string> keys)
    {
        var resource = keys.Select(k => $"SampleLocker:{k}").ToList();
        return this.GrabLock(resource, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(3), TimeSpan.FromMilliseconds(300));
    }
    /// <summary>
    /// 加鎖
    /// </summary>
    /// <param name="resource">需要加鎖的KEY</param>
    /// <param name="ttl">鎖有效存活時間</param>
    /// <param name="waitTime">要不到鎖的等待間</param>
    /// <param name="retryTime">要不到鎖重新要鎖等待時間</param>
    /// <returns></returns>
    private IRedisLck GrabLock(IEnumerable<string> resource, TimeSpan ttl, TimeSpan waitTime, TimeSpan retryTime)
        => this.factory.CreateLock(resource, ttl, waitTime, retryTime);
}

/// <summary>
/// 非關連式資料庫服務
/// </summary>
internal class NoSqlService
{
    const string redisConn = @"localhost:6379";
    private static Lazy<ConnectionMultiplexer> lazyRedisConnections;
    public static ConnectionMultiplexer RedisConnections
    {
        get
        {
            if (lazyRedisConnections == null)
            {
                lazyRedisConnections = new Lazy<ConnectionMultiplexer>(() =>
                {
                    var maxTimeout = 5 * 1000;
                    var options = ConfigurationOptions.Parse(redisConn);
                    options.AbortOnConnectFail = false;
                    options.ClientName = "RedisLock";
                    options.SyncTimeout = maxTimeout;
                    options.AsyncTimeout = maxTimeout;
                    var muxer = ConnectionMultiplexer.Connect(options);
                    muxer.ConnectionFailed += (sender, e) =>
                    {
                        Console.WriteLine("redis failed: " + EndPointCollection.ToString(e.EndPoint) + "/" + e.ConnectionType);
                    };
                    muxer.ConnectionRestored += (sender, e) =>
                    {
                        Console.WriteLine("redis restored: " + EndPointCollection.ToString(e.EndPoint) + "/" + e.ConnectionType);
                    };
                    return muxer;
                });
            }
            return lazyRedisConnections.Value;
        }
    }
    private static Lazy<RedisLockFactory> lazyDistributedLockService;
    public static RedisLockFactory DistributedLockService
    {
        get
        {
            if (lazyDistributedLockService == null)
            {
                lazyDistributedLockService = new Lazy<RedisLockFactory>(() =>
                {
                    return new RedisLockFactory(RedisConnections);
                });
            }
            return lazyDistributedLockService.Value;
        }
    }
}

static void Main(string[] args)
{
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
}
```