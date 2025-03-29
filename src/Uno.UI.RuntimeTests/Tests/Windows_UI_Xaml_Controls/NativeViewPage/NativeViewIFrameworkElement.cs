#pragma warning disable 108, 114

using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
#if __APPLE_UIKIT__
using _NativeBase = UIKit.UISwitch;
#elif __ANDROID__
using _NativeBase = AndroidX.AppCompat.Widget.AppCompatCheckBox;
#else
using _NativeBase = Microsoft.UI.Xaml.Controls.CheckBox; // No native views on other platforms
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class NativeViewIFrameworkElement : _NativeBase
#if __APPLE_UIKIT__ || __ANDROID__
		, DependencyObject, IFrameworkElement
#endif
	{
#if __ANDROID__
		public NativeViewIFrameworkElement() : base(ContextHelper.Current)
		{

		}
#endif

		public object MyValue
		{
			get { return (object)GetValue(MyValueProperty); }
			set { SetValue(MyValueProperty, value); }
		}

		// Using a DependencyProperty as the backing store for MyValue.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty MyValueProperty =
			DependencyProperty.Register("MyValue", typeof(object), typeof(NativeViewIFrameworkElement), new PropertyMetadata(0));


#if __APPLE_UIKIT__ || __ANDROID__

		private void OnUnloaded() { }
		private void OnLoading() { }
		private void OnLoaded() { }

		public VerticalAlignment VerticalAlignment
		{
			get { return (VerticalAlignment)GetValue(VerticalAlignmentProperty); }
			set { SetValue(VerticalAlignmentProperty, value); }
		}

		// Using a DependencyProperty as the backing store for VerticalAlignment.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty VerticalAlignmentProperty =
			DependencyProperty.Register("VerticalAlignment", typeof(VerticalAlignment), typeof(NativeViewIFrameworkElement), new PropertyMetadata(VerticalAlignment.Top));



		public HorizontalAlignment HorizontalAlignment
		{
			get { return (HorizontalAlignment)GetValue(HorizontalAlignmentProperty); }
			set { SetValue(HorizontalAlignmentProperty, value); }
		}

		// Using a DependencyProperty as the backing store for HorizontalAlignment.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty HorizontalAlignmentProperty =
			DependencyProperty.Register("HorizontalAlignment", typeof(HorizontalAlignment), typeof(NativeViewIFrameworkElement), new PropertyMetadata(HorizontalAlignment.Left));



		public DependencyObject Parent => this.GetParent() as DependencyObject;

		public string Name { get; set; }
		public bool IsEnabled { get; set; }
		public Visibility Visibility { get; set; }
		public Thickness Margin { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		public double MinWidth { get; set; }
		public double MinHeight { get; set; }
		public double MaxWidth { get; set; }
		public double MaxHeight { get; set; }

		public double ActualWidth => 0;

		public double ActualHeight => 0;

		public double Opacity { get; set; }
		public Style Style { get; set; }
		public Brush Background { get; set; }
		public Transform RenderTransform { get; set; }
		public Point RenderTransformOrigin { get; set; }
		public TransitionCollection Transitions { get; set; }

		public Uri BaseUri => new Uri("ms-appx://local");

		public int? RenderPhase { get; set; }

		public Rect AppliedFrame => default;

		public bool IsParsing { get; set; }

#if __APPLE_UIKIT__ || __ANDROID__
		public event RoutedEventHandler Loaded { add { } remove { } }
		public event RoutedEventHandler Unloaded { add { } remove { } }
		public event EventHandler<object> LayoutUpdated { add { } remove { } }
		public event SizeChangedEventHandler SizeChanged { add { } remove { } }
#endif

		public Size AdjustArrange(Size finalSize) => finalSize;

		public void ApplyBindingPhase(int phase) { }

		public void CreationComplete() { }

		public object FindName(string name) => null;

		public string GetAccessibilityInnerText() => null;

		public AutomationPeer GetAutomationPeer() => null;

		public void SetSubviewsNeedLayout() { }
#endif
	}
}
