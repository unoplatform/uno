#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class StackPanel : global::Windows.UI.Xaml.Controls.Panel,global::Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo,global::Windows.UI.Xaml.Controls.IInsertionPanel
	{
		// Skipping already declared property Orientation
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AreScrollSnapPointsRegular
		{
			get
			{
				return (bool)this.GetValue(AreScrollSnapPointsRegularProperty);
			}
			set
			{
				this.SetValue(AreScrollSnapPointsRegularProperty, value);
			}
		}
		#endif
		// Skipping already declared property Padding
		// Skipping already declared property CornerRadius
		// Skipping already declared property BorderThickness
		// Skipping already declared property BorderBrush
		// Skipping already declared property Spacing
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
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AreHorizontalSnapPointsRegular
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StackPanel.AreHorizontalSnapPointsRegular is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AreVerticalSnapPointsRegular
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StackPanel.AreVerticalSnapPointsRegular is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty AreScrollSnapPointsRegularProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(AreScrollSnapPointsRegular), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.StackPanel), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Skipping already declared property OrientationProperty
		// Skipping already declared property BorderBrushProperty
		// Skipping already declared property BorderThicknessProperty
		// Skipping already declared property CornerRadiusProperty
		// Skipping already declared property PaddingProperty
		// Skipping already declared property SpacingProperty
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty BackgroundSizingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(BackgroundSizing), typeof(global::Windows.UI.Xaml.Controls.BackgroundSizing), 
			typeof(global::Windows.UI.Xaml.Controls.StackPanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.BackgroundSizing)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.StackPanel.StackPanel()
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.StackPanel()
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.AreScrollSnapPointsRegular.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.AreScrollSnapPointsRegular.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Orientation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Orientation.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderBrush.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderBrush.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderThickness.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderThickness.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.CornerRadius.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.CornerRadius.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Padding.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Padding.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Spacing.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Spacing.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BackgroundSizing.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BackgroundSizing.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.AreHorizontalSnapPointsRegular.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.AreVerticalSnapPointsRegular.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.HorizontalSnapPointsChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.HorizontalSnapPointsChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.VerticalSnapPointsChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.VerticalSnapPointsChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<float> GetIrregularSnapPoints( global::Windows.UI.Xaml.Controls.Orientation orientation,  global::Windows.UI.Xaml.Controls.Primitives.SnapPointsAlignment alignment)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<float> StackPanel.GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  float GetRegularSnapPoints( global::Windows.UI.Xaml.Controls.Orientation orientation,  global::Windows.UI.Xaml.Controls.Primitives.SnapPointsAlignment alignment, out float offset)
		{
			throw new global::System.NotImplementedException("The member float StackPanel.GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void GetInsertionIndexes( global::Windows.Foundation.Point position, out int first, out int second)
		{
			throw new global::System.NotImplementedException("The member void StackPanel.GetInsertionIndexes(Point position, out int first, out int second) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BackgroundSizingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.SpacingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderBrushProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderThicknessProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.CornerRadiusProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.PaddingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.AreScrollSnapPointsRegularProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.OrientationProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::System.EventHandler<object> HorizontalSnapPointsChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.StackPanel", "event EventHandler<object> StackPanel.HorizontalSnapPointsChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.StackPanel", "event EventHandler<object> StackPanel.HorizontalSnapPointsChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::System.EventHandler<object> VerticalSnapPointsChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.StackPanel", "event EventHandler<object> StackPanel.VerticalSnapPointsChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.StackPanel", "event EventHandler<object> StackPanel.VerticalSnapPointsChanged");
			}
		}
		#endif
		// Processing: Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo
		// Processing: Windows.UI.Xaml.Controls.IInsertionPanel
	}
}
