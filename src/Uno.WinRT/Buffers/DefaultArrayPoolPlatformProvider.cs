using System;
using System.Diagnostics;
using Windows.System;

namespace Uno.Buffers
{
	/// <summary>
	/// Platform features provider for <see cref="ArrayPool{T}"/>
	/// </summary>
	/// <remarks>
	/// Used primarily to allow for deterministic testing of ArrayPool trimming features.
	/// </remarks>
	internal class DefaultArrayPoolPlatformProvider : IArrayPoolPlatformProvider
	{
		private static readonly bool _canUseMemoryManager;
		private readonly Stopwatch _watch = new();
		private TimeSpan _lastMemorySnapshot = TimeSpan.FromMinutes(-30);
		private Windows.System.AppMemoryUsageLevel _appMemoryUsageUsageLevel;
		private readonly static TimeSpan MemoryUsageUpdateResolution = TimeSpan.FromSeconds(5);

		static DefaultArrayPoolPlatformProvider()
		{
			_canUseMemoryManager =
				Windows.Foundation.Metadata.ApiInformation.IsPropertyPresent("Windows.System.MemoryManager, Uno", "AppMemoryUsage")
				&& Windows.System.MemoryManager.IsAvailable;
		}

		public DefaultArrayPoolPlatformProvider()
		{
			_watch.Start();
		}

		public TimeSpan Now
			=> _watch.Elapsed;

		public bool CanUseMemoryManager
			=> _canUseMemoryManager;

		public AppMemoryUsageLevel AppMemoryUsageLevel
		{
			get
			{
				UpdateMemoryUsage();
				return _appMemoryUsageUsageLevel;
			}
		}

		private void UpdateMemoryUsage()
		{
			if (Now - _lastMemorySnapshot > MemoryUsageUpdateResolution)
			{
				_lastMemorySnapshot = Now;
				_appMemoryUsageUsageLevel = Windows.System.MemoryManager.AppMemoryUsageLevel;
			}
		}

		public void RegisterTrimCallback(Func<object, bool> callback, object target)
		{
			Windows.Foundation.Gen2GcCallback.Register(callback, target);
		}
	}
}
