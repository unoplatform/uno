#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Resources.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ResourceContext 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<string> Languages
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> ResourceContext.Languages is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "IReadOnlyList<string> ResourceContext.Languages");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.IObservableMap<string, string> QualifierValues
		{
			get
			{
				throw new global::System.NotImplementedException("The member IObservableMap<string, string> ResourceContext.QualifierValues is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public ResourceContext() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "ResourceContext.ResourceContext()");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceContext.ResourceContext()
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceContext.QualifierValues.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Reset()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.Reset()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Reset( global::System.Collections.Generic.IEnumerable<string> qualifierNames)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.Reset(IEnumerable<string> qualifierNames)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void OverrideToMatch( global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Resources.Core.ResourceQualifier> result)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.OverrideToMatch(IEnumerable<ResourceQualifier> result)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Resources.Core.ResourceContext Clone()
		{
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.Clone() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceContext.Languages.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceContext.Languages.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Resources.Core.ResourceContext GetForUIContext( global::Windows.UI.UIContext context)
		{
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.GetForUIContext(UIContext context) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetGlobalQualifierValue( string key,  string value,  global::Windows.ApplicationModel.Resources.Core.ResourceQualifierPersistence persistence)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.SetGlobalQualifierValue(string key, string value, ResourceQualifierPersistence persistence)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Resources.Core.ResourceContext GetForCurrentView()
		{
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.GetForCurrentView() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void SetGlobalQualifierValue( string key,  string value)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.SetGlobalQualifierValue(string key, string value)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ResetGlobalQualifierValues()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.ResetGlobalQualifierValues()");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static void ResetGlobalQualifierValues( global::System.Collections.Generic.IEnumerable<string> qualifierNames)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceContext", "void ResourceContext.ResetGlobalQualifierValues(IEnumerable<string> qualifierNames)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Resources.Core.ResourceContext GetForViewIndependentUse()
		{
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.GetForViewIndependentUse() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Resources.Core.ResourceContext CreateMatchingContext( global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Resources.Core.ResourceQualifier> result)
		{
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.CreateMatchingContext(IEnumerable<ResourceQualifier> result) is not implemented in Uno.");
		}
		#endif
	}
}
