#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Display.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class DisplayState 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsReadOnly
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayState.IsReadOnly is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsStale
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool DisplayState.IsStale is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IDictionary<global::System.Guid, object> Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member IDictionary<Guid, object> DisplayState.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Display.Core.DisplayTarget> Targets
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<DisplayTarget> DisplayState.Targets is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Devices.Display.Core.DisplayView> Views
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<DisplayView> DisplayState.Views is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.Display.Core.DisplayState.IsReadOnly.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayState.IsStale.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayState.Targets.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayState.Views.get
		// Forced skipping of method Windows.Devices.Display.Core.DisplayState.Properties.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayPath ConnectTarget( global::Windows.Devices.Display.Core.DisplayTarget target)
		{
			throw new global::System.NotImplementedException("The member DisplayPath DisplayState.ConnectTarget(DisplayTarget target) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayPath ConnectTarget( global::Windows.Devices.Display.Core.DisplayTarget target,  global::Windows.Devices.Display.Core.DisplayView view)
		{
			throw new global::System.NotImplementedException("The member DisplayPath DisplayState.ConnectTarget(DisplayTarget target, DisplayView view) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanConnectTargetToView( global::Windows.Devices.Display.Core.DisplayTarget target,  global::Windows.Devices.Display.Core.DisplayView view)
		{
			throw new global::System.NotImplementedException("The member bool DisplayState.CanConnectTargetToView(DisplayTarget target, DisplayView view) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayView GetViewForTarget( global::Windows.Devices.Display.Core.DisplayTarget target)
		{
			throw new global::System.NotImplementedException("The member DisplayView DisplayState.GetViewForTarget(DisplayTarget target) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayPath GetPathForTarget( global::Windows.Devices.Display.Core.DisplayTarget target)
		{
			throw new global::System.NotImplementedException("The member DisplayPath DisplayState.GetPathForTarget(DisplayTarget target) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void DisconnectTarget( global::Windows.Devices.Display.Core.DisplayTarget target)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.Display.Core.DisplayState", "void DisplayState.DisconnectTarget(DisplayTarget target)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayStateOperationResult TryFunctionalize( global::Windows.Devices.Display.Core.DisplayStateFunctionalizeOptions options)
		{
			throw new global::System.NotImplementedException("The member DisplayStateOperationResult DisplayState.TryFunctionalize(DisplayStateFunctionalizeOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayStateOperationResult TryApply( global::Windows.Devices.Display.Core.DisplayStateApplyOptions options)
		{
			throw new global::System.NotImplementedException("The member DisplayStateOperationResult DisplayState.TryApply(DisplayStateApplyOptions options) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.Display.Core.DisplayState Clone()
		{
			throw new global::System.NotImplementedException("The member DisplayState DisplayState.Clone() is not implemented in Uno.");
		}
		#endif
	}
}
