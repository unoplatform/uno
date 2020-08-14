#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MemoryManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static ulong AppMemoryUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong MemoryManager.AppMemoryUsage is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.AppMemoryUsageLevel AppMemoryUsageLevel
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppMemoryUsageLevel MemoryManager.AppMemoryUsageLevel is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static ulong AppMemoryUsageLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong MemoryManager.AppMemoryUsageLimit is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static ulong ExpectedAppMemoryUsageLimit
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong MemoryManager.ExpectedAppMemoryUsageLimit is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.MemoryManager.ExpectedAppMemoryUsageLimit.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool TrySetAppMemoryUsageLimit( ulong value)
		{
			throw new global::System.NotImplementedException("The member bool MemoryManager.TrySetAppMemoryUsageLimit(ulong value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.AppMemoryReport GetAppMemoryReport()
		{
			throw new global::System.NotImplementedException("The member AppMemoryReport MemoryManager.GetAppMemoryReport() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.ProcessMemoryReport GetProcessMemoryReport()
		{
			throw new global::System.NotImplementedException("The member ProcessMemoryReport MemoryManager.GetProcessMemoryReport() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.System.MemoryManager.AppMemoryUsage.get
		// Forced skipping of method Windows.System.MemoryManager.AppMemoryUsageLimit.get
		// Forced skipping of method Windows.System.MemoryManager.AppMemoryUsageLevel.get
		// Forced skipping of method Windows.System.MemoryManager.AppMemoryUsageIncreased.add
		// Forced skipping of method Windows.System.MemoryManager.AppMemoryUsageIncreased.remove
		// Forced skipping of method Windows.System.MemoryManager.AppMemoryUsageDecreased.add
		// Forced skipping of method Windows.System.MemoryManager.AppMemoryUsageDecreased.remove
		// Forced skipping of method Windows.System.MemoryManager.AppMemoryUsageLimitChanging.add
		// Forced skipping of method Windows.System.MemoryManager.AppMemoryUsageLimitChanging.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> AppMemoryUsageDecreased
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.MemoryManager", "event EventHandler<object> MemoryManager.AppMemoryUsageDecreased");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.MemoryManager", "event EventHandler<object> MemoryManager.AppMemoryUsageDecreased");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<object> AppMemoryUsageIncreased
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.MemoryManager", "event EventHandler<object> MemoryManager.AppMemoryUsageIncreased");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.MemoryManager", "event EventHandler<object> MemoryManager.AppMemoryUsageIncreased");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.System.AppMemoryUsageLimitChangingEventArgs> AppMemoryUsageLimitChanging
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.MemoryManager", "event EventHandler<AppMemoryUsageLimitChangingEventArgs> MemoryManager.AppMemoryUsageLimitChanging");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.MemoryManager", "event EventHandler<AppMemoryUsageLimitChangingEventArgs> MemoryManager.AppMemoryUsageLimitChanging");
			}
		}
		#endif
	}
}
