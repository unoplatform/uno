#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Media.Media3D
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class Transform3D : global::Windows.UI.Xaml.DependencyObject
	{
		#if false || __IOS__ || false || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		protected Transform3D() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Media.Media3D.Transform3D", "Transform3D.Transform3D()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Media.Media3D.Transform3D.Transform3D()
	}
}
