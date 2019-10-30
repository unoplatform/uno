#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || NET461 || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProgressRing 
	{
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
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
		#if __ANDROID__ || __IOS__ || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.ProgressRingTemplateSettings TemplateSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member ProgressRingTemplateSettings ProgressRing.TemplateSettings is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsActiveProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsActive", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ProgressRing), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || NET461 || false || false
		[global::Uno.NotImplemented]
		public ProgressRing() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ProgressRing", "ProgressRing.ProgressRing()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressRing.ProgressRing()
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressRing.IsActive.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressRing.IsActive.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressRing.TemplateSettings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressRing.IsActiveProperty.get
	}
}
