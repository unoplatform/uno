#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PersonPictureTemplateSettings : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Media.ImageBrush ActualImageBrush
		{
			get
			{
				throw new global::System.NotImplementedException("The member ImageBrush PersonPictureTemplateSettings.ActualImageBrush is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ImageBrush%20PersonPictureTemplateSettings.ActualImageBrush");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ActualInitials
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PersonPictureTemplateSettings.ActualInitials is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PersonPictureTemplateSettings.ActualInitials");
			}
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.PersonPictureTemplateSettings.ActualInitials.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.PersonPictureTemplateSettings.ActualImageBrush.get
	}
}
