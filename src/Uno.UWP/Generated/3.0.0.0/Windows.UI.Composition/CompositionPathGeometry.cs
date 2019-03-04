#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Composition
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CompositionPathGeometry : global::Windows.UI.Composition.CompositionGeometry
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Composition.CompositionPath Path
		{
			get
			{
				throw new global::System.NotImplementedException("The member CompositionPath CompositionPathGeometry.Path is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Composition.CompositionPathGeometry", "CompositionPath CompositionPathGeometry.Path");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Composition.CompositionPathGeometry.Path.get
		// Forced skipping of method Windows.UI.Composition.CompositionPathGeometry.Path.set
	}
}
