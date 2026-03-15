namespace ReTime_Testing.Tests.Services;

/// <summary>
/// AbsoluteTimeService 类的单元测试
/// 测试绝对时间服务的各种场景
/// </summary>
public class AbsoluteTimeServiceTests
{
    [Fact]
    public void Constructor_应该正确初始化()
    {
        // Arrange & Act
        var service = new AbsoluteTimeService();

        // Assert
        service.Should().NotBeNull();
        service.IsCloudSynchronized.Should().BeFalse();
        service.GetCurrentTime().Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetCurrentTime_应该返回单调递增的时间()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var time1 = service.GetCurrentTime();
        Thread.Sleep(100); // 等待 100ms

        // Act
        var time2 = service.GetCurrentTime();

        // Assert
        time2.Should().BeAfter(time1);
        var elapsed = time2 - time1;
        elapsed.Should().BeCloseTo(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50));
    }

    [Fact]
    public void GetCurrentTime_连续调用应该平滑递增()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var times = new List<DateTime>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            times.Add(service.GetCurrentTime());
            Thread.Sleep(10);
        }

        // Assert
        for (int i = 1; i < times.Count; i++)
        {
            times[i].Should().BeAfter(times[i - 1]);
        }
    }

    [Fact]
    public void Calibrate_应该正确更新基准时间()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var oldTime = service.GetCurrentTime();
        var cloudTime = oldTime.AddMinutes(10);

        var eventRaised = false;
        TimeJumpedEventArgs? capturedArgs = null;
        service.TimeJumped += (sender, args) =>
        {
            eventRaised = true;
            capturedArgs = args;
        };

        // Act
        service.Calibrate(cloudTime);
        var newTime = service.GetCurrentTime();

        // Assert
        eventRaised.Should().BeTrue();
        capturedArgs.Should().NotBeNull();
        capturedArgs!.OldTime.Should().BeCloseTo(oldTime, TimeSpan.FromMilliseconds(100));
        capturedArgs.NewTime.Should().Be(cloudTime);
        newTime.Should().BeCloseTo(cloudTime, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void Calibrate_校准后IsCloudSynchronized应该为True()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var cloudTime = service.GetCurrentTime().AddMinutes(10);

        // Act
        service.Calibrate(cloudTime);

        // Assert
        service.IsCloudSynchronized.Should().BeTrue();
    }

    [Fact]
    public void Calibrate_多次校准应该正确工作()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var eventCount = 0;
        service.TimeJumped += (sender, args) => eventCount++;

        // Act
        service.Calibrate(service.GetCurrentTime().AddMinutes(10));
        Thread.Sleep(50);
        service.Calibrate(service.GetCurrentTime().AddMinutes(20));
        Thread.Sleep(50);
        service.Calibrate(service.GetCurrentTime().AddMinutes(30));

        // Assert
        eventCount.Should().Be(3);
        service.IsCloudSynchronized.Should().BeTrue();
    }

    [Fact]
    public void TimeJumped_事件参数应该正确()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var oldTime = service.GetCurrentTime();
        var newTime = oldTime.AddHours(1);

        TimeJumpedEventArgs? capturedArgs = null;
        service.TimeJumped += (sender, args) => capturedArgs = args;

        // Act
        service.Calibrate(newTime);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.OldTime.Should().BeCloseTo(oldTime, TimeSpan.FromMilliseconds(500));
        capturedArgs.NewTime.Should().Be(newTime);
        capturedArgs.Offset.Should().BeCloseTo(TimeSpan.FromHours(1), TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Calibrate_时间向后跳跃应该触发事件()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var oldTime = service.GetCurrentTime();
        var newTime = oldTime.AddHours(-1); // 向后跳跃

        var eventRaised = false;
        TimeJumpedEventArgs? capturedArgs = null;
        service.TimeJumped += (sender, args) =>
        {
            eventRaised = true;
            capturedArgs = args;
        };

        // Act
        service.Calibrate(newTime);

        // Assert
        eventRaised.Should().BeTrue();
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Offset.Should().BeNegative();
        // 允许一定的误差，因为在调用 GetCurrentTime 和 Calibrate 之间有时间差
        capturedArgs.Offset.Should().BeCloseTo(TimeSpan.FromHours(-1), TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Calibrate_时间向前跳跃应该触发事件()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var oldTime = service.GetCurrentTime();
        var newTime = oldTime.AddHours(2); // 向前跳跃

        var eventRaised = false;
        TimeJumpedEventArgs? capturedArgs = null;
        service.TimeJumped += (sender, args) =>
        {
            eventRaised = true;
            capturedArgs = args;
        };

        // Act
        service.Calibrate(newTime);

        // Assert
        eventRaised.Should().BeTrue();
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Offset.Should().BePositive();
        // 允许一定的误差，因为在调用 GetCurrentTime 和 Calibrate 之间有时间差
        capturedArgs.Offset.Should().BeCloseTo(TimeSpan.FromHours(2), TimeSpan.FromMilliseconds(500));
    }

    [Fact]
    public void Calibrate_校准到当前时间Offset应该接近零()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var currentTime = service.GetCurrentTime();

        TimeJumpedEventArgs? capturedArgs = null;
        service.TimeJumped += (sender, args) => capturedArgs = args;

        // Act
        service.Calibrate(currentTime);

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Offset.Duration().Should().BeLessThan(TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void GetCurrentTime_校准后时间应该平滑递增()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var baseTime = service.GetCurrentTime().AddHours(1);
        service.Calibrate(baseTime);

        var times = new List<DateTime>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            times.Add(service.GetCurrentTime());
            Thread.Sleep(50);
        }

        // Assert
        times[0].Should().BeCloseTo(baseTime, TimeSpan.FromMilliseconds(100));
        for (int i = 1; i < times.Count; i++)
        {
            times[i].Should().BeAfter(times[i - 1]);
        }
    }

    [Fact]
    public void Calibrate_多个订阅者都应该收到事件()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var count1 = 0;
        var count2 = 0;
        var count3 = 0;

        service.TimeJumped += (sender, args) => count1++;
        service.TimeJumped += (sender, args) => count2++;
        service.TimeJumped += (sender, args) => count3++;

        // Act
        service.Calibrate(service.GetCurrentTime().AddMinutes(10));

        // Assert
        count1.Should().Be(1);
        count2.Should().Be(1);
        count3.Should().Be(1);
    }

    [Fact]
    public void Calibrate_取消订阅后不应收到事件()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var count = 0;

        EventHandler<TimeJumpedEventArgs> handler = (sender, args) => count++;
        service.TimeJumped += handler;

        // Act
        service.Calibrate(service.GetCurrentTime().AddMinutes(10));
        service.TimeJumped -= handler;
        service.Calibrate(service.GetCurrentTime().AddMinutes(20));

        // Assert
        count.Should().Be(1); // 只有第一次校准触发事件
    }

    [Fact]
    public void Calibrate_并发调用应该线程安全()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var tasks = new List<Task>();
        var exceptions = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        var eventCount = 0;

        service.TimeJumped += (sender, args) => Interlocked.Increment(ref eventCount);

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    var time = DateTime.Now.AddMinutes(i);
                    service.Calibrate(time);
                    var currentTime = service.GetCurrentTime();
                    // 验证时间在合理范围内
                    if (Math.Abs((currentTime - time).TotalSeconds) > 1)
                    {
                        throw new InvalidOperationException($"时间偏差过大: {currentTime} vs {time}");
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.Should().BeEmpty();
        eventCount.Should().Be(100);
        service.IsCloudSynchronized.Should().BeTrue();
    }

    [Fact]
    public void GetCurrentTime_并发调用应该线程安全()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var tasks = new List<Task>();
        var times = new System.Collections.Concurrent.ConcurrentBag<DateTime>();
        var exceptions = new System.Collections.Concurrent.ConcurrentQueue<Exception>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                try
                {
                    times.Add(service.GetCurrentTime());
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Assert
        exceptions.Should().BeEmpty();
        times.Count.Should().Be(100);
        service.IsCloudSynchronized.Should().BeFalse(); // 未校准
    }

    [Fact]
    public void Calibrate_校准后立即调用GetCurrentTime应该返回校准后的时间()
    {
        // Arrange
        var service = new AbsoluteTimeService();
        var cloudTime = new DateTime(2026, 3, 15, 10, 30, 0);

        // Act
        service.Calibrate(cloudTime);
        var currentTime = service.GetCurrentTime();

        // Assert
        currentTime.Should().BeCloseTo(cloudTime, TimeSpan.FromMilliseconds(100));
    }

    [Fact]
    public void TimeJumpedEventArgs_应该正确计算Offset()
    {
        // Arrange
        var oldTime = new DateTime(2026, 3, 15, 10, 0, 0);
        var newTime = new DateTime(2026, 3, 15, 12, 30, 0);

        // Act
        var args = new TimeJumpedEventArgs(oldTime, newTime);

        // Assert
        args.OldTime.Should().Be(oldTime);
        args.NewTime.Should().Be(newTime);
        args.Offset.Should().Be(TimeSpan.FromHours(2.5));
    }

    [Fact]
    public void TimeJumpedEventArgs_向后跳跃Offset应该为负()
    {
        // Arrange
        var oldTime = new DateTime(2026, 3, 15, 12, 0, 0);
        var newTime = new DateTime(2026, 3, 15, 10, 0, 0);

        // Act
        var args = new TimeJumpedEventArgs(oldTime, newTime);

        // Assert
        args.Offset.Should().BeNegative();
        args.Offset.Should().Be(TimeSpan.FromHours(-2));
    }

    [Fact]
    public void TimeJumpedEventArgs_无跳跃Offset应该为零()
    {
        // Arrange
        var time = new DateTime(2026, 3, 15, 10, 0, 0);

        // Act
        var args = new TimeJumpedEventArgs(time, time);

        // Assert
        args.Offset.Should().Be(TimeSpan.Zero);
    }
}