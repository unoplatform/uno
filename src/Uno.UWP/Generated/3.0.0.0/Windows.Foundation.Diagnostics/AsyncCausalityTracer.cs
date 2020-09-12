#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AsyncCausalityTracer 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void TraceOperationCreation( global::Windows.Foundation.Diagnostics.CausalityTraceLevel traceLevel,  global::Windows.Foundation.Diagnostics.CausalitySource source,  global::System.Guid platformId,  ulong operationId,  string operationName,  ulong relatedContext)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.AsyncCausalityTracer", "void AsyncCausalityTracer.TraceOperationCreation(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, string operationName, ulong relatedContext)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void TraceOperationCompletion( global::Windows.Foundation.Diagnostics.CausalityTraceLevel traceLevel,  global::Windows.Foundation.Diagnostics.CausalitySource source,  global::System.Guid platformId,  ulong operationId,  global::Windows.Foundation.AsyncStatus status)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.AsyncCausalityTracer", "void AsyncCausalityTracer.TraceOperationCompletion(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, AsyncStatus status)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void TraceOperationRelation( global::Windows.Foundation.Diagnostics.CausalityTraceLevel traceLevel,  global::Windows.Foundation.Diagnostics.CausalitySource source,  global::System.Guid platformId,  ulong operationId,  global::Windows.Foundation.Diagnostics.CausalityRelation relation)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.AsyncCausalityTracer", "void AsyncCausalityTracer.TraceOperationRelation(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, CausalityRelation relation)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void TraceSynchronousWorkStart( global::Windows.Foundation.Diagnostics.CausalityTraceLevel traceLevel,  global::Windows.Foundation.Diagnostics.CausalitySource source,  global::System.Guid platformId,  ulong operationId,  global::Windows.Foundation.Diagnostics.CausalitySynchronousWork work)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.AsyncCausalityTracer", "void AsyncCausalityTracer.TraceSynchronousWorkStart(CausalityTraceLevel traceLevel, CausalitySource source, Guid platformId, ulong operationId, CausalitySynchronousWork work)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void TraceSynchronousWorkCompletion( global::Windows.Foundation.Diagnostics.CausalityTraceLevel traceLevel,  global::Windows.Foundation.Diagnostics.CausalitySource source,  global::Windows.Foundation.Diagnostics.CausalitySynchronousWork work)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.AsyncCausalityTracer", "void AsyncCausalityTracer.TraceSynchronousWorkCompletion(CausalityTraceLevel traceLevel, CausalitySource source, CausalitySynchronousWork work)");
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.AsyncCausalityTracer.TracingStatusChanged.add
		// Forced skipping of method Windows.Foundation.Diagnostics.AsyncCausalityTracer.TracingStatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static event global::System.EventHandler<global::Windows.Foundation.Diagnostics.TracingStatusChangedEventArgs> TracingStatusChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.AsyncCausalityTracer", "event EventHandler<TracingStatusChangedEventArgs> AsyncCausalityTracer.TracingStatusChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Foundation.Diagnostics.AsyncCausalityTracer", "event EventHandler<TracingStatusChangedEventArgs> AsyncCausalityTracer.TracingStatusChanged");
			}
		}
		#endif
	}
}
