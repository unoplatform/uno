#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemCpuUsageReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan IdleTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SystemCpuUsageReport.IdleTime is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20SystemCpuUsageReport.IdleTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan KernelTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SystemCpuUsageReport.KernelTime is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20SystemCpuUsageReport.KernelTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan UserTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan SystemCpuUsageReport.UserTime is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TimeSpan%20SystemCpuUsageReport.UserTime");
			}
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.SystemCpuUsageReport.KernelTime.get
		// Forced skipping of method Windows.System.Diagnostics.SystemCpuUsageReport.UserTime.get
		// Forced skipping of method Windows.System.Diagnostics.SystemCpuUsageReport.IdleTime.get
	}
}
