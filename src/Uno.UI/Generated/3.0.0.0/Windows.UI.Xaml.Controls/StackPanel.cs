#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class StackPanel : global::Windows.UI.Xaml.Controls.Panel,global::Windows.UI.Xaml.Controls.Primitives.IScrollSnapPointsInfo,global::Windows.UI.Xaml.Controls.IInsertionPanel
	{
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Orientation Orientation
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Orientation)this.GetValue(OrientationProperty);
			}
			set
			{
				this.SetValue(OrientationProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
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
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Thickness Padding
		{
			get
			{
				return (global::Windows.UI.Xaml.Thickness)this.GetValue(PaddingProperty);
			}
			set
			{
				this.SetValue(PaddingProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.CornerRadius CornerRadius
		{
			get
			{
				return (global::Windows.UI.Xaml.CornerRadius)this.GetValue(CornerRadiusProperty);
			}
			set
			{
				this.SetValue(CornerRadiusProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Thickness BorderThickness
		{
			get
			{
				return (global::Windows.UI.Xaml.Thickness)this.GetValue(BorderThicknessProperty);
			}
			set
			{
				this.SetValue(BorderThicknessProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Media.Brush BorderBrush
		{
			get
			{
				return (global::Windows.UI.Xaml.Media.Brush)this.GetValue(BorderBrushProperty);
			}
			set
			{
				this.SetValue(BorderBrushProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double Spacing
		{
			get
			{
				return (double)this.GetValue(SpacingProperty);
			}
			set
			{
				this.SetValue(SpacingProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool AreHorizontalSnapPointsRegular
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StackPanel.AreHorizontalSnapPointsRegular is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool AreVerticalSnapPointsRegular
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool StackPanel.AreVerticalSnapPointsRegular is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AreScrollSnapPointsRegularProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AreScrollSnapPointsRegular", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.StackPanel), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OrientationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Orientation", typeof(global::Windows.UI.Xaml.Controls.Orientation), 
			typeof(global::Windows.UI.Xaml.Controls.StackPanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Orientation)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BorderBrushProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BorderBrush", typeof(global::Windows.UI.Xaml.Media.Brush), 
			typeof(global::Windows.UI.Xaml.Controls.StackPanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BorderThicknessProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BorderThickness", typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.StackPanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CornerRadiusProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CornerRadius", typeof(global::Windows.UI.Xaml.CornerRadius), 
			typeof(global::Windows.UI.Xaml.Controls.StackPanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.CornerRadius)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PaddingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Padding", typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.StackPanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SpacingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Spacing", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.StackPanel), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public StackPanel() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.StackPanel", "StackPanel.StackPanel()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.StackPanel()
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.AreScrollSnapPointsRegular.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.AreScrollSnapPointsRegular.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Orientation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Orientation.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.AreHorizontalSnapPointsRegular.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.AreVerticalSnapPointsRegular.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.HorizontalSnapPointsChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.HorizontalSnapPointsChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.VerticalSnapPointsChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.VerticalSnapPointsChanged.remove
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<float> GetIrregularSnapPoints( global::Windows.UI.Xaml.Controls.Orientation orientation,  global::Windows.UI.Xaml.Controls.Primitives.SnapPointsAlignment alignment)
		{
			throw new global::System.NotImplementedException("The member IReadOnlyList<float> StackPanel.GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  float GetRegularSnapPoints( global::Windows.UI.Xaml.Controls.Orientation orientation,  global::Windows.UI.Xaml.Controls.Primitives.SnapPointsAlignment alignment, out float offset)
		{
			throw new global::System.NotImplementedException("The member float StackPanel.GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderBrush.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderBrush.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderThickness.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderThickness.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.CornerRadius.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.CornerRadius.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Padding.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Padding.set
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  void GetInsertionIndexes( global::Windows.Foundation.Point position, out int first, out int second)
		{
			throw new global::System.NotImplementedException("The member void StackPanel.GetInsertionIndexes(Point position, out int first, out int second) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Spacing.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.Spacing.set
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.SpacingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderBrushProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.BorderThicknessProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.CornerRadiusProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.PaddingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.AreScrollSnapPointsRegularProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.StackPanel.OrientationProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> HorizontalSnapPointsChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.StackPanel", "event EventHandler<object> StackPanel.HorizontalSnapPointsChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.StackPanel", "event EventHandler<object> StackPanel.HorizontalSnapPointsChanged");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  event global::System.EventHandler<object> VerticalSnapPointsChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.StackPanel", "event EventHandler<object> StackPanel.VerticalSnapPointsChanged");
			}
			[global::Uno.NotImplemented]
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
