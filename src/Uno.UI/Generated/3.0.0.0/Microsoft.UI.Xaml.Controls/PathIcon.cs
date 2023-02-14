#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if false || false || false || false || false || __NETSTD_REFERENCE__ || false
	[global::Uno.NotImplemented("__NETSTD_REFERENCE__")]
	#endif
	public  partial class PathIcon : global::Microsoft.UI.Xaml.Controls.IconElement
	{
		#if false || false || false || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("__NETSTD_REFERENCE__")]
		public  global::Microsoft.UI.Xaml.Media.Geometry Data
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Media.Geometry)this.GetValue(DataProperty);
			}
			set
			{
				this.SetValue(DataProperty, value);
			}
		}
		#endif
		#if false || false || false || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("__NETSTD_REFERENCE__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty DataProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(Data), typeof(global::Microsoft.UI.Xaml.Media.Geometry), 
			typeof(global::Microsoft.UI.Xaml.Controls.PathIcon), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Media.Geometry)));
		#endif
		#if false || false || false || false || false || __NETSTD_REFERENCE__ || false
		[global::Uno.NotImplemented("__NETSTD_REFERENCE__")]
		public PathIcon() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.PathIcon", "PathIcon.PathIcon()");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.PathIcon.PathIcon()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.PathIcon.Data.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.PathIcon.Data.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.PathIcon.DataProperty.get
	}
}
