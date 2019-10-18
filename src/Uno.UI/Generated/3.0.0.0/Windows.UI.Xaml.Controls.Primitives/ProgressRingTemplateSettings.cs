#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if __ANDROID__ || __IOS__ || NET461 || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProgressRingTemplateSettings : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  double EllipseDiameter
		{
			get
			{
				throw new global::System.NotImplementedException("The member double ProgressRingTemplateSettings.EllipseDiameter is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Thickness EllipseOffset
		{
			get
			{
				throw new global::System.NotImplementedException("The member Thickness ProgressRingTemplateSettings.EllipseOffset is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || false || __MACOS__
		[global::Uno.NotImplemented]
		public  double MaxSideLength
		{
			get
			{
				throw new global::System.NotImplementedException("The member double ProgressRingTemplateSettings.MaxSideLength is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ProgressRingTemplateSettings.EllipseDiameter.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ProgressRingTemplateSettings.EllipseOffset.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ProgressRingTemplateSettings.MaxSideLength.get
	}
}
