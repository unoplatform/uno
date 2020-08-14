#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PathIcon : global::Windows.UI.Xaml.Controls.IconElement
	{
		#if false || false || false || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("__NETSTD_REFERENCE__")]
		public  global::Windows.UI.Xaml.Media.Geometry Data
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Geometry)this.GetValue(DataProperty);
			}
			set
			{
				this.SetValue(DataProperty, value);
			}
		}
		#endif
		#if false || false || false || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("__NETSTD_REFERENCE__")]
		public static global::Windows.UI.Xaml.DependencyProperty DataProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Data), typeof(global::Windows.UI.Xaml.Media.Geometry), 
			typeof(global::Windows.UI.Xaml.Controls.PathIcon), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Geometry)));
		#endif
		#if false || false || false || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("__NETSTD_REFERENCE__")]
		public PathIcon() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.PathIcon", "PathIcon.PathIcon()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.PathIcon.PathIcon()
		// Forced skipping of method Windows.UI.Xaml.Controls.PathIcon.Data.get
		// Forced skipping of method Windows.UI.Xaml.Controls.PathIcon.Data.set
		// Forced skipping of method Windows.UI.Xaml.Controls.PathIcon.DataProperty.get
	}
}
