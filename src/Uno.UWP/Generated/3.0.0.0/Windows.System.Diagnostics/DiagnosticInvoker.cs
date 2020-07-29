#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DiagnosticInvoker 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DiagnosticInvoker.IsSupported is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.System.Diagnostics.DiagnosticActionResult, global::Windows.System.Diagnostics.DiagnosticActionState> RunDiagnosticActionAsync( global::Windows.Data.Json.JsonObject context)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DiagnosticActionResult, DiagnosticActionState> DiagnosticInvoker.RunDiagnosticActionAsync(JsonObject context) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperationWithProgress<global::Windows.System.Diagnostics.DiagnosticActionResult, global::Windows.System.Diagnostics.DiagnosticActionState> RunDiagnosticActionFromStringAsync( string context)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperationWithProgress<DiagnosticActionResult, DiagnosticActionState> DiagnosticInvoker.RunDiagnosticActionFromStringAsync(string context) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.DiagnosticInvoker GetDefault()
		{
			throw new global::System.NotImplementedException("The member DiagnosticInvoker DiagnosticInvoker.GetDefault() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.System.Diagnostics.DiagnosticInvoker GetForUser( global::Windows.System.User user)
		{
			throw new global::System.NotImplementedException("The member DiagnosticInvoker DiagnosticInvoker.GetForUser(User user) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.System.Diagnostics.DiagnosticInvoker.IsSupported.get
	}
}
