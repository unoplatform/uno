#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkToolbarToolButton : global::Windows.UI.Xaml.Controls.RadioButton
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  bool IsExtensionGlyphShown
		{
			get
			{
				return (bool)this.GetValue(IsExtensionGlyphShownProperty);
			}
			set
			{
				this.SetValue(IsExtensionGlyphShownProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.InkToolbarTool ToolKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member InkToolbarTool InkToolbarToolButton.ToolKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsExtensionGlyphShownProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsExtensionGlyphShown), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.InkToolbarToolButton), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarToolButton.ToolKind.get
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarToolButton.IsExtensionGlyphShown.get
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarToolButton.IsExtensionGlyphShown.set
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarToolButton.IsExtensionGlyphShownProperty.get
	}
}
