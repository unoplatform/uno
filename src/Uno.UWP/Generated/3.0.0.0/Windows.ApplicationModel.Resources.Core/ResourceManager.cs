#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Resources.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ResourceManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, global::Windows.ApplicationModel.Resources.Core.ResourceMap> AllResourceMaps
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, ResourceMap> ResourceManager.AllResourceMaps is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Resources.Core.ResourceContext DefaultContext
		{
			get
			{
				throw new global::System.NotImplementedException("The member ResourceContext ResourceManager.DefaultContext is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Resources.Core.ResourceMap MainResourceMap
		{
			get
			{
				throw new global::System.NotImplementedException("The member ResourceMap ResourceManager.MainResourceMap is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.ApplicationModel.Resources.Core.ResourceManager Current
		{
			get
			{
				throw new global::System.NotImplementedException("The member ResourceManager ResourceManager.Current is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceManager.MainResourceMap.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceManager.AllResourceMaps.get
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceManager.DefaultContext.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void LoadPriFiles( global::System.Collections.Generic.IEnumerable<global::Windows.Storage.IStorageFile> files)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceManager", "void ResourceManager.LoadPriFiles(IEnumerable<IStorageFile> files)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void UnloadPriFiles( global::System.Collections.Generic.IEnumerable<global::Windows.Storage.IStorageFile> files)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceManager", "void ResourceManager.UnloadPriFiles(IEnumerable<IStorageFile> files)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Resources.Core.NamedResource> GetAllNamedResourcesForPackage( string packageName,  global::Windows.ApplicationModel.Resources.Core.ResourceLayoutInfo resourceLayoutInfo)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<NamedResource> ResourceManager.GetAllNamedResourcesForPackage(string packageName, ResourceLayoutInfo resourceLayoutInfo) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Resources.Core.ResourceMap> GetAllSubtreesForPackage( string packageName,  global::Windows.ApplicationModel.Resources.Core.ResourceLayoutInfo resourceLayoutInfo)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<ResourceMap> ResourceManager.GetAllSubtreesForPackage(string packageName, ResourceLayoutInfo resourceLayoutInfo) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Resources.Core.ResourceManager.Current.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static bool IsResourceReference( string resourceReference)
		{
			throw new global::System.NotImplementedException("The member bool ResourceManager.IsResourceReference(string resourceReference) is not implemented in Uno.");
		}
		#endif
	}
}
