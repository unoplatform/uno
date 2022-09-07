#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CoreIndependentInputSourceController : global::System.IDisposable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsTransparentForUncontrolledInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreIndependentInputSourceController.IsTransparentForUncontrolledInput is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSourceController", "bool CoreIndependentInputSourceController.IsTransparentForUncontrolledInput");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsPalmRejectionEnabled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CoreIndependentInputSourceController.IsPalmRejectionEnabled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSourceController", "bool CoreIndependentInputSourceController.IsPalmRejectionEnabled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CoreIndependentInputSource Source
		{
			get
			{
				throw new global::System.NotImplementedException("The member CoreIndependentInputSource CoreIndependentInputSourceController.Source is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSourceController.IsTransparentForUncontrolledInput.get
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSourceController.IsTransparentForUncontrolledInput.set
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSourceController.IsPalmRejectionEnabled.get
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSourceController.IsPalmRejectionEnabled.set
		// Forced skipping of method Windows.UI.Core.CoreIndependentInputSourceController.Source.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetControlledInput( global::Windows.UI.Core.CoreInputDeviceTypes inputTypes)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSourceController", "void CoreIndependentInputSourceController.SetControlledInput(CoreInputDeviceTypes inputTypes)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void SetControlledInput( global::Windows.UI.Core.CoreInputDeviceTypes inputTypes,  global::Windows.UI.Core.CoreIndependentInputFilters required,  global::Windows.UI.Core.CoreIndependentInputFilters excluded)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSourceController", "void CoreIndependentInputSourceController.SetControlledInput(CoreInputDeviceTypes inputTypes, CoreIndependentInputFilters required, CoreIndependentInputFilters excluded)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Core.CoreIndependentInputSourceController", "void CoreIndependentInputSourceController.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Core.CoreIndependentInputSourceController CreateForVisual( global::Windows.UI.Composition.Visual visual)
		{
			throw new global::System.NotImplementedException("The member CoreIndependentInputSourceController CoreIndependentInputSourceController.CreateForVisual(Visual visual) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Core.CoreIndependentInputSourceController CreateForIVisualElement( global::Windows.UI.Composition.IVisualElement visualElement)
		{
			throw new global::System.NotImplementedException("The member CoreIndependentInputSourceController CoreIndependentInputSourceController.CreateForIVisualElement(IVisualElement visualElement) is not implemented in Uno.");
		}
		#endif
		// Processing: System.IDisposable
	}
}
