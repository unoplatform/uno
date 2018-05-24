#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProgressBar 
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool ShowPaused
		{
			get
			{
				return (bool)this.GetValue(ShowPausedProperty);
			}
			set
			{
				this.SetValue(ShowPausedProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool ShowError
		{
			get
			{
				return (bool)this.GetValue(ShowErrorProperty);
			}
			set
			{
				this.SetValue(ShowErrorProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool IsIndeterminate
		{
			get
			{
				return (bool)this.GetValue(IsIndeterminateProperty);
			}
			set
			{
				this.SetValue(IsIndeterminateProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.ProgressBarTemplateSettings TemplateSettings
		{
			get
			{
				throw new global::System.NotImplementedException("The member ProgressBarTemplateSettings ProgressBar.TemplateSettings is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsIndeterminateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsIndeterminate", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ProgressBar), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ShowErrorProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ShowError", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ProgressBar), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ShowPausedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ShowPaused", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ProgressBar), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ProgressBar() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ProgressBar", "ProgressBar.ProgressBar()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.ProgressBar()
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.IsIndeterminate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.IsIndeterminate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.ShowError.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.ShowError.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.ShowPaused.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.ShowPaused.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.TemplateSettings.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.IsIndeterminateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.ShowErrorProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ProgressBar.ShowPausedProperty.get
	}
}
