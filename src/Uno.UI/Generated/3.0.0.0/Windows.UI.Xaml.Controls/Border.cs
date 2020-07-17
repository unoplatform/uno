#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Border : global::Windows.UI.Xaml.FrameworkElement
	{
		// Skipping already declared property Padding
		// Skipping already declared property CornerRadius
		// Skipping already declared property ChildTransitions
		// Skipping already declared property Child
		// Skipping already declared property BorderThickness
		// Skipping already declared property BorderBrush
		// Skipping already declared property Background
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.BrushTransition BackgroundTransition
		{
			get
			{
				throw new global::System.NotImplementedException("The member BrushTransition Border.BackgroundTransition is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Border", "BrushTransition Border.BackgroundTransition");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.BackgroundSizing BackgroundSizing
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.BackgroundSizing)this.GetValue(BackgroundSizingProperty);
			}
			set
			{
				this.SetValue(BackgroundSizingProperty, value);
			}
		}
		#endif
		// Skipping already declared property BackgroundProperty
		// Skipping already declared property BorderBrushProperty
		// Skipping already declared property BorderThicknessProperty
		// Skipping already declared property ChildTransitionsProperty
		// Skipping already declared property CornerRadiusProperty
		// Skipping already declared property PaddingProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty BackgroundSizingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(BackgroundSizing), typeof(global::Windows.UI.Xaml.Controls.BackgroundSizing), 
			typeof(global::Windows.UI.Xaml.Controls.Border), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.BackgroundSizing)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.Border.Border()
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.Border()
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BorderBrush.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BorderBrush.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BorderThickness.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BorderThickness.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.Background.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.Background.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.CornerRadius.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.CornerRadius.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.Padding.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.Padding.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.Child.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.Child.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.ChildTransitions.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.ChildTransitions.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BackgroundSizing.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BackgroundSizing.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BackgroundTransition.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BackgroundTransition.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BackgroundSizingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BorderBrushProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BorderThicknessProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.BackgroundProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.CornerRadiusProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.PaddingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Border.ChildTransitionsProperty.get
	}
}
