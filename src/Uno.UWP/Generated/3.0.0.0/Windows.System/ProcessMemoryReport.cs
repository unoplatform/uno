#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProcessMemoryReport 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong PrivateWorkingSetUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong ProcessMemoryReport.PrivateWorkingSetUsage is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20ProcessMemoryReport.PrivateWorkingSetUsage");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ulong TotalWorkingSetUsage
		{
			get
			{
				throw new global::System.NotImplementedException("The member ulong ProcessMemoryReport.TotalWorkingSetUsage is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ulong%20ProcessMemoryReport.TotalWorkingSetUsage");
			}
		}
		#endif
		// Forced skipping of method Windows.System.ProcessMemoryReport.PrivateWorkingSetUsage.get
		// Forced skipping of method Windows.System.ProcessMemoryReport.TotalWorkingSetUsage.get
	}
}
