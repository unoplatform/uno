using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
#if XAMARIN_ANDROID
using Android.Views;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#elif __WASM__
using View = Windows.UI.Xaml.UIElement;
#else
using View = System.Object;
#endif

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Transform :  Based on the WinRT Transform
	/// 
	/// https://msdn.microsoft.com/en-us/library/system.windows.media.transform(v=vs.110).aspx
	/// </summary>
	public abstract partial class Transform : GeneralTransform
	{
		internal virtual void OnViewSizeChanged(Size oldSize, Size newSize)
		{
			Update();
		}

#if !__WASM__
		protected virtual void Update()
		{
			UpdatePartial();
		}
#endif

		partial void UpdatePartial();

		View _view;

		/// <summary>
		/// Transforms are attached to a View
		/// </summary>
		internal View View
		{
			get
			{
				return _view;
			}
			set
			{
				var view = _view;
				_view = value;
				if (value != null)
				{
					OnAttachedToView();
					OnAttachedToViewPartial(_view);
				}
				else
				{
					OnDetachedFromViewPartial(view);
				}
			}
		}

		protected virtual void OnAttachedToView()
		{

		}

		partial void OnAttachedToViewPartial(View view);

		partial void OnDetachedFromViewPartial(View view);

		/// <summary>
		/// The <see cref="FrameworkElement.RenderTransformOrigin"/> of the targetted view.
		/// </summary>
		internal virtual Foundation.Point Origin { get; set; }
	}
}


