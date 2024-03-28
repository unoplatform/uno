#nullable enable

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Uno.UI.Dispatching;
using Windows.Foundation.Metadata;
using Windows.System;

namespace Windows.UI.Xaml;

class FrameworkTemplatePoolDefaultPlatformProvider : IFrameworkTemplatePoolPlatformProvider
{
	private static readonly bool _canUseMemoryManager;
	private readonly Stopwatch _watch = new Stopwatch();
	private TimeSpan _lastMemorySnapshot = TimeSpan.FromMinutes(-30);
	private ulong _appMemoryUsageLimit;
	private ulong _appMemoryUsage;
	private readonly static TimeSpan MemoryUsageUpdateResolution = TimeSpan.FromSeconds(5);

	static FrameworkTemplatePoolDefaultPlatformProvider()
	{
		_canUseMemoryManager =
			ApiInformation.IsPropertyPresent("Windows.System.MemoryManager", "AppMemoryUsage")
			&& MemoryManager.AppMemoryUsage > 0;
	}

	public FrameworkTemplatePoolDefaultPlatformProvider()
		=> _watch.Start();

	public TimeSpan Now
		=> _watch.Elapsed;

	public virtual bool CanUseMemoryManager
		=> _canUseMemoryManager;

	public ulong AppMemoryUsage
	{
		get
		{
			UpdateMemoryUsage();
			return _appMemoryUsage;
		}
	}

	public ulong AppMemoryUsageLimit
	{
		get
		{
			UpdateMemoryUsage();
			return _appMemoryUsageLimit;
		}
	}

	private void UpdateMemoryUsage()
	{
		if (Now - _lastMemorySnapshot > MemoryUsageUpdateResolution)
		{
			_lastMemorySnapshot = Now;
			_appMemoryUsageLimit = MemoryManager.AppMemoryUsageLimit;
			_appMemoryUsage = MemoryManager.AppMemoryUsage;
		}
	}

	public Task Delay(TimeSpan duration)
		=> Task.Delay(duration);

	public void Schedule(Action action)
		=> NativeDispatcher.Main.Enqueue(action, NativeDispatcherPriority.Idle);
}
