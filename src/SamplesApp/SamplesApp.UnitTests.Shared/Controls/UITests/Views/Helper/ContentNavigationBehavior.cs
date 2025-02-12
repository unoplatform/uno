#if WINAPPSDK
using Windows.ApplicationModel.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
#elif HAS_UNO || UNO_REFERENCE_API
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
#else
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows;
#endif

namespace Uno.UI.Samples.Helper
{
	public static class ContentNavigationBehavior
	{
		public static bool GetIsAttached(DependencyObject obj)
		{
			return (bool)obj.GetValue(IsAttachedProperty);
		}

		public static void SetIsAttached(DependencyObject obj, bool value)
		{
			obj.SetValue(IsAttachedProperty, value);
		}

		public static DependencyProperty IsAttachedProperty { get; } =
			DependencyProperty.RegisterAttached(
				"IsAttached",
				typeof(bool),
				typeof(ContentNavigationBehavior),
				new PropertyMetadataHelper(new PropertyChangedCallback(OnIsAttached)));

		public static bool GetCanNavigateBack(DependencyObject obj)
		{
			return (bool)obj.GetValue(CanNavigateBackProperty);
		}

		public static void SetCanNavigateBack(DependencyObject obj, bool value)
		{
			obj.SetValue(CanNavigateBackProperty, value);
		}

		public static DependencyProperty CanNavigateBackProperty { get; } =
			DependencyProperty.RegisterAttached(
				"CanNavigateBack",
				typeof(bool),
				typeof(ContentNavigationBehavior),
				new PropertyMetadata(false));

		private static void OnIsAttached(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var elem = d as FrameworkElement;

			// We set the true/false value based on IsHitTestVisible (since it's 
			// bound and manipulated at run-time in SampleChooserControl.xaml)
			SetCanNavigateBack(elem, elem.IsHitTestVisible);

			elem.Loaded += (sender, args) =>
			{
				var element = sender as FrameworkElement;

				if (element != null)
				{
					// On first load of the app, we set the property when the element is done Loading
					SetCanNavigateBack(element, element.IsHitTestVisible);
				}
			};
		}
	}
}
