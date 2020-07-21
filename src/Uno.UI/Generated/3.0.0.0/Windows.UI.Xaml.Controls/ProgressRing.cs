#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProgressRing 
	{
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public  bool IsActive
		{
			get
			{
				return (bool)this.GetValue(IsActiveProperty);
			}
			set
			{
				this.SetValue(IsActiveProperty, value);
			}
		}
		#endif
		#if false || false || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Primitives.ProgressRingTemplateSettings TemplateSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member ProgressRingTemplateSettings ProgressRing.TemplateSettings is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false || false || false
		[global::Uno.NotImplemented("NET461")]
		public static global::Windows.UI.Xaml.DependencyProperty IsActiveProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsActive), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ProgressRing), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.ProgressRing.ProgressRing()
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressRing.ProgressRing()
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressRing.IsActive.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressRing.IsActive.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressRing.TemplateSettings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressRing.IsActiveProperty.get
	}
}
