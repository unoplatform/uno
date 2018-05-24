#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class RelativePanel : global::Windows.UI.Xaml.Controls.Panel
	{
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
		public static global::Windows.UI.Xaml.DependencyProperty AboveProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"Above", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignBottomWithPanelProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignBottomWithPanel", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignBottomWithProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignBottomWith", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignHorizontalCenterWithPanelProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignHorizontalCenterWithPanel", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignHorizontalCenterWithProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignHorizontalCenterWith", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignLeftWithPanelProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignLeftWithPanel", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignLeftWithProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignLeftWith", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignRightWithPanelProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignRightWithPanel", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignRightWithProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignRightWith", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignTopWithPanelProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignTopWithPanel", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignTopWithProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignTopWith", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignVerticalCenterWithPanelProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignVerticalCenterWithPanel", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AlignVerticalCenterWithProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"AlignVerticalCenterWith", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BelowProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"Below", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BorderBrushProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BorderBrush", typeof(global::Windows.UI.Xaml.Media.Brush), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Media.Brush)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty BorderThicknessProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"BorderThickness", typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CornerRadiusProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CornerRadius", typeof(global::Windows.UI.Xaml.CornerRadius), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.CornerRadius)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LeftOfProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"LeftOf", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PaddingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Padding", typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RightOfProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"RightOf", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.RelativePanel), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public RelativePanel() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.RelativePanel", "RelativePanel.RelativePanel()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.RelativePanel()
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.BorderBrush.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.BorderBrush.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.BorderThickness.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.BorderThickness.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.CornerRadius.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.CornerRadius.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.Padding.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.Padding.set
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.LeftOfProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object GetLeftOf( global::Windows.UI.Xaml.UIElement element)
		{
			return (object)element.GetValue(LeftOfProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetLeftOf( global::Windows.UI.Xaml.UIElement element,  object value)
		{
			element.SetValue(LeftOfProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AboveProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object GetAbove( global::Windows.UI.Xaml.UIElement element)
		{
			return (object)element.GetValue(AboveProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAbove( global::Windows.UI.Xaml.UIElement element,  object value)
		{
			element.SetValue(AboveProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.RightOfProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object GetRightOf( global::Windows.UI.Xaml.UIElement element)
		{
			return (object)element.GetValue(RightOfProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetRightOf( global::Windows.UI.Xaml.UIElement element,  object value)
		{
			element.SetValue(RightOfProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.BelowProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object GetBelow( global::Windows.UI.Xaml.UIElement element)
		{
			return (object)element.GetValue(BelowProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetBelow( global::Windows.UI.Xaml.UIElement element,  object value)
		{
			element.SetValue(BelowProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignHorizontalCenterWithProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object GetAlignHorizontalCenterWith( global::Windows.UI.Xaml.UIElement element)
		{
			return (object)element.GetValue(AlignHorizontalCenterWithProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignHorizontalCenterWith( global::Windows.UI.Xaml.UIElement element,  object value)
		{
			element.SetValue(AlignHorizontalCenterWithProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignVerticalCenterWithProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object GetAlignVerticalCenterWith( global::Windows.UI.Xaml.UIElement element)
		{
			return (object)element.GetValue(AlignVerticalCenterWithProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignVerticalCenterWith( global::Windows.UI.Xaml.UIElement element,  object value)
		{
			element.SetValue(AlignVerticalCenterWithProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignLeftWithProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object GetAlignLeftWith( global::Windows.UI.Xaml.UIElement element)
		{
			return (object)element.GetValue(AlignLeftWithProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignLeftWith( global::Windows.UI.Xaml.UIElement element,  object value)
		{
			element.SetValue(AlignLeftWithProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignTopWithProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object GetAlignTopWith( global::Windows.UI.Xaml.UIElement element)
		{
			return (object)element.GetValue(AlignTopWithProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignTopWith( global::Windows.UI.Xaml.UIElement element,  object value)
		{
			element.SetValue(AlignTopWithProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignRightWithProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object GetAlignRightWith( global::Windows.UI.Xaml.UIElement element)
		{
			return (object)element.GetValue(AlignRightWithProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignRightWith( global::Windows.UI.Xaml.UIElement element,  object value)
		{
			element.SetValue(AlignRightWithProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignBottomWithProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static object GetAlignBottomWith( global::Windows.UI.Xaml.UIElement element)
		{
			return (object)element.GetValue(AlignBottomWithProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignBottomWith( global::Windows.UI.Xaml.UIElement element,  object value)
		{
			element.SetValue(AlignBottomWithProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignLeftWithPanelProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static bool GetAlignLeftWithPanel( global::Windows.UI.Xaml.UIElement element)
		{
			return (bool)element.GetValue(AlignLeftWithPanelProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignLeftWithPanel( global::Windows.UI.Xaml.UIElement element,  bool value)
		{
			element.SetValue(AlignLeftWithPanelProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignTopWithPanelProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static bool GetAlignTopWithPanel( global::Windows.UI.Xaml.UIElement element)
		{
			return (bool)element.GetValue(AlignTopWithPanelProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignTopWithPanel( global::Windows.UI.Xaml.UIElement element,  bool value)
		{
			element.SetValue(AlignTopWithPanelProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignRightWithPanelProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static bool GetAlignRightWithPanel( global::Windows.UI.Xaml.UIElement element)
		{
			return (bool)element.GetValue(AlignRightWithPanelProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignRightWithPanel( global::Windows.UI.Xaml.UIElement element,  bool value)
		{
			element.SetValue(AlignRightWithPanelProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignBottomWithPanelProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static bool GetAlignBottomWithPanel( global::Windows.UI.Xaml.UIElement element)
		{
			return (bool)element.GetValue(AlignBottomWithPanelProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignBottomWithPanel( global::Windows.UI.Xaml.UIElement element,  bool value)
		{
			element.SetValue(AlignBottomWithPanelProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignHorizontalCenterWithPanelProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static bool GetAlignHorizontalCenterWithPanel( global::Windows.UI.Xaml.UIElement element)
		{
			return (bool)element.GetValue(AlignHorizontalCenterWithPanelProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignHorizontalCenterWithPanel( global::Windows.UI.Xaml.UIElement element,  bool value)
		{
			element.SetValue(AlignHorizontalCenterWithPanelProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.AlignVerticalCenterWithPanelProperty.get
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static bool GetAlignVerticalCenterWithPanel( global::Windows.UI.Xaml.UIElement element)
		{
			return (bool)element.GetValue(AlignVerticalCenterWithPanelProperty);
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static void SetAlignVerticalCenterWithPanel( global::Windows.UI.Xaml.UIElement element,  bool value)
		{
			element.SetValue(AlignVerticalCenterWithPanelProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.BorderBrushProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.BorderThicknessProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.CornerRadiusProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RelativePanel.PaddingProperty.get
	}
}
