#if IS_UNO
using System;
using System.Collections.Generic;
using System.Text;

#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = System.Object;
#endif

namespace Windows.UI.Xaml
{
	/// <summary>
	/// A support element for the DeferLoadStrategy Lazy Xaml directive.
	/// </summary>
	/// <remarks>This control is added in the visual tree, in place of the original content.</remarks>
	public partial class ElementStub : FrameworkElement
    {
		/// <summary>
		/// A function that will create the actual view.
		/// </summary>
		public Func<View> ContentBuilder { get; set; }

		protected override void OnVisibilityChanged(Visibility oldValue, Visibility newValue)
		{
			base.OnVisibilityChanged(oldValue, newValue);

			if (ContentBuilder != null
				&& oldValue == Visibility.Collapsed 
				&& newValue == Visibility.Visible
			)
			{
				Materialize();
			}
		}

		protected override void OnLoaded()
		{
			base.OnLoaded();

			if (ContentBuilder != null
				&& Visibility == Visibility.Visible
			)
			{
				Materialize();
			}
		}

		public void Materialize()
		{
			var newContent = MaterializeContent();

			var targetDependencyObject = newContent as DependencyObject;

			if (targetDependencyObject != null)
			{
				var visibilityProperty = GetVisibilityProperty(newContent);

				// Set the visibility at the same precedence it was currently set with on the stub.
				var precedence = this.GetCurrentHighestValuePrecedence(visibilityProperty);

				targetDependencyObject.SetValue(visibilityProperty, Visibility.Visible, precedence);
			}
		}

		private static DependencyProperty GetVisibilityProperty(View view)
		{
			if(view is FrameworkElement)
			{
				return VisibilityProperty;
			}
			else
			{
				return DependencyProperty.GetProperty(view.GetType(), nameof(Visibility));
			}
		}
	}
}
#endif