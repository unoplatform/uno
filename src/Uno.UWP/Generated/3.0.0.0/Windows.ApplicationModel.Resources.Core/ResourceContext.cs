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
				throw new global::System.NotImplementedException("The member IReadOnlyList<string> ResourceContext.Languages is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3Cstring%3E%20ResourceContext.Languages");
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
				throw new global::System.NotImplementedException("The member IObservableMap<string, string> ResourceContext.QualifierValues is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IObservableMap%3Cstring%2C%20string%3E%20ResourceContext.QualifierValues");
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
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.Clone() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ResourceContext%20ResourceContext.Clone%28%29");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceContext.Languages.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceContext.Languages.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Resources.Core.ResourceContext GetForUIContext( global::Windows.UI.UIContext context)
		{
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.GetForUIContext(UIContext context) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ResourceContext%20ResourceContext.GetForUIContext%28UIContext%20context%29");
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
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.GetForCurrentView() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ResourceContext%20ResourceContext.GetForCurrentView%28%29");
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
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.GetForViewIndependentUse() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ResourceContext%20ResourceContext.GetForViewIndependentUse%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Resources.Core.ResourceContext CreateMatchingContext( global::System.Collections.Generic.IEnumerable<global::Windows.ApplicationModel.Resources.Core.ResourceQualifier> result)
		{
			throw new global::System.NotImplementedException("The member ResourceContext ResourceContext.CreateMatchingContext(IEnumerable<ResourceQualifier> result) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ResourceContext%20ResourceContext.CreateMatchingContext%28IEnumerable%3CResourceQualifier%3E%20result%29");
		}
		#endif
	}
}
