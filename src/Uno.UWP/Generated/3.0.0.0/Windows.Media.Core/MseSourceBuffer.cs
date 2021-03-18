#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MseSourceBuffer 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan TimestampOffset
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MseSourceBuffer.TimestampOffset is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "TimeSpan MseSourceBuffer.TimestampOffset");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Core.MseAppendMode Mode
		{
			get
			{
				throw new global::System.NotImplementedException("The member MseAppendMode MseSourceBuffer.Mode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "MseAppendMode MseSourceBuffer.Mode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan AppendWindowStart
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan MseSourceBuffer.AppendWindowStart is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "TimeSpan MseSourceBuffer.AppendWindowStart");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.TimeSpan? AppendWindowEnd
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan? MseSourceBuffer.AppendWindowEnd is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "TimeSpan? MseSourceBuffer.AppendWindowEnd");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Core.MseTimeRange> Buffered
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MseTimeRange> MseSourceBuffer.Buffered is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsUpdating
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MseSourceBuffer.IsUpdating is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.UpdateStarting.add
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.UpdateStarting.remove
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.Updated.add
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.Updated.remove
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.UpdateEnded.add
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.UpdateEnded.remove
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.ErrorOccurred.add
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.ErrorOccurred.remove
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.Aborted.add
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.Aborted.remove
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.Mode.get
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.Mode.set
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.IsUpdating.get
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.Buffered.get
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.TimestampOffset.get
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.TimestampOffset.set
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.AppendWindowStart.get
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.AppendWindowStart.set
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.AppendWindowEnd.get
		// Forced skipping of method Windows.Media.Core.MseSourceBuffer.AppendWindowEnd.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AppendBuffer( global::Windows.Storage.Streams.IBuffer buffer)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "void MseSourceBuffer.AppendBuffer(IBuffer buffer)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AppendStream( global::Windows.Storage.Streams.IInputStream stream)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "void MseSourceBuffer.AppendStream(IInputStream stream)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void AppendStream( global::Windows.Storage.Streams.IInputStream stream,  ulong maxSize)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "void MseSourceBuffer.AppendStream(IInputStream stream, ulong maxSize)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Abort()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "void MseSourceBuffer.Abort()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Remove( global::System.TimeSpan start,  global::System.TimeSpan? end)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "void MseSourceBuffer.Remove(TimeSpan start, TimeSpan? end)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MseSourceBuffer, object> Aborted
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "event TypedEventHandler<MseSourceBuffer, object> MseSourceBuffer.Aborted");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "event TypedEventHandler<MseSourceBuffer, object> MseSourceBuffer.Aborted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MseSourceBuffer, object> ErrorOccurred
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "event TypedEventHandler<MseSourceBuffer, object> MseSourceBuffer.ErrorOccurred");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "event TypedEventHandler<MseSourceBuffer, object> MseSourceBuffer.ErrorOccurred");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MseSourceBuffer, object> UpdateEnded
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "event TypedEventHandler<MseSourceBuffer, object> MseSourceBuffer.UpdateEnded");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "event TypedEventHandler<MseSourceBuffer, object> MseSourceBuffer.UpdateEnded");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MseSourceBuffer, object> UpdateStarting
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "event TypedEventHandler<MseSourceBuffer, object> MseSourceBuffer.UpdateStarting");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "event TypedEventHandler<MseSourceBuffer, object> MseSourceBuffer.UpdateStarting");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.Media.Core.MseSourceBuffer, object> Updated
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "event TypedEventHandler<MseSourceBuffer, object> MseSourceBuffer.Updated");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Media.Core.MseSourceBuffer", "event TypedEventHandler<MseSourceBuffer, object> MseSourceBuffer.Updated");
			}
		}
		#endif
	}
}
