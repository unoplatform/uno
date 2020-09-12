#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionObject : global::System.IDisposable,global::Windows.UI.Composition.IAnimationObject
	{
		// Skipping already declared property Compositor
		// Skipping already declared property Dispatcher
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.CompositionPropertySet Properties
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionPropertySet CompositionObject.Properties is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.ImplicitAnimationCollection ImplicitAnimations
		{
			get
			{
				throw new global::System.NotImplementedException("The member ImplicitAnimationCollection CompositionObject.ImplicitAnimations is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionObject", "ImplicitAnimationCollection CompositionObject.ImplicitAnimations");
			}
		}
		#endif
		// Skipping already declared property Comment
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.DispatcherQueue DispatcherQueue
		{
			get
			{
				throw new global::System.NotImplementedException("The member DispatcherQueue CompositionObject.DispatcherQueue is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionObject.Compositor.get
		// Forced skipping of method Windows.UI.Composition.CompositionObject.Dispatcher.get
		// Forced skipping of method Windows.UI.Composition.CompositionObject.Properties.get
		// Skipping already declared method Windows.UI.Composition.CompositionObject.StartAnimation(string, Windows.UI.Composition.CompositionAnimation)
		// Skipping already declared method Windows.UI.Composition.CompositionObject.StopAnimation(string)
		// Forced skipping of method Windows.UI.Composition.CompositionObject.Comment.get
		// Forced skipping of method Windows.UI.Composition.CompositionObject.Comment.set
		// Forced skipping of method Windows.UI.Composition.CompositionObject.ImplicitAnimations.get
		// Forced skipping of method Windows.UI.Composition.CompositionObject.ImplicitAnimations.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StartAnimationGroup( global::Windows.UI.Composition.ICompositionAnimationBase value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionObject", "void CompositionObject.StartAnimationGroup(ICompositionAnimationBase value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void StopAnimationGroup( global::Windows.UI.Composition.ICompositionAnimationBase value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionObject", "void CompositionObject.StopAnimationGroup(ICompositionAnimationBase value)");
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionObject.DispatcherQueue.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Composition.AnimationController TryGetAnimationController( string propertyName)
		{
			throw new global::System.NotImplementedException("The member AnimationController CompositionObject.TryGetAnimationController(string propertyName) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Dispose()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionObject", "void CompositionObject.Dispose()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void PopulatePropertyInfo( string propertyName,  global::Windows.UI.Composition.AnimationPropertyInfo propertyInfo)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionObject", "void CompositionObject.PopulatePropertyInfo(string propertyName, AnimationPropertyInfo propertyInfo)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void StartAnimationWithIAnimationObject( global::Windows.UI.Composition.IAnimationObject target,  string propertyName,  global::Windows.UI.Composition.CompositionAnimation animation)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionObject", "void CompositionObject.StartAnimationWithIAnimationObject(IAnimationObject target, string propertyName, CompositionAnimation animation)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void StartAnimationGroupWithIAnimationObject( global::Windows.UI.Composition.IAnimationObject target,  global::Windows.UI.Composition.ICompositionAnimationBase animation)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionObject", "void CompositionObject.StartAnimationGroupWithIAnimationObject(IAnimationObject target, ICompositionAnimationBase animation)");
		}
		#endif
		// Processing: System.IDisposable
		// Processing: Windows.UI.Composition.IAnimationObject
	}
}
