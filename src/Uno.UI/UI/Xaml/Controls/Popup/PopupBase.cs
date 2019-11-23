using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Media;
#if XAMARIN_IOS
using CoreGraphics;
using UIKit;
using View = UIKit.UIView;
#elif __MACOS__
using CoreGraphics;
using AppKit;
using View = AppKit.NSView;
#elif XAMARIN_ANDROID
using View = Android.Views.View;
#elif NET461 || __WASM__
using View = Windows.UI.Xaml.FrameworkElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class PopupBase : FrameworkElement, IPopup
	{
		private IDisposable _openPopupRegistration;
		private bool _childHasOwnDataContext;

		public event EventHandler<object> Closed;
		public event EventHandler<object> Opened;

		/// <summary>
		/// Defines a custom layouter which overrides the default placement logic of the <see cref="PopupPanel"/>
		/// </summary>
		internal IDynamicPopupLayouter CustomLayouter { get; set; }

		protected override void OnUnloaded()
		{
			IsOpen = false;
			base.OnUnloaded();
		}

		/// <inheritdoc />
		protected override Size MeasureOverride(Size availableSize)
		{
			// As the Child is NOT part of the visual tree, it does not have to be measured,
			// and the result size of this Popup is always 0,0

			return new Size();
		}

		/// <inheritdoc />
		protected override Size ArrangeOverride(Size finalSize)
		{
			// As the Child is NOT part of the visual tree, it does not have to be arranged,
			// and the result size of this Popup is always 0,0

			return new Size();
		}

		partial void OnIsOpenChangedPartial(bool oldIsOpen, bool newIsOpen)
		{
			if (newIsOpen)
			{
				_openPopupRegistration = VisualTreeHelper.RegisterOpenPopup(this);
				Opened?.Invoke(this, newIsOpen);
			}
			else
			{
				_openPopupRegistration?.Dispose();
				Closed?.Invoke(this, newIsOpen);
			}
		}

		partial void OnChildChangedPartial(View oldChild, View newChild)
		{
			if (oldChild is IDependencyObjectStoreProvider provider && !_childHasOwnDataContext)
			{
				provider.Store.ClearValue(provider.Store.DataContextProperty, DependencyPropertyValuePrecedences.Local);
				provider.Store.ClearValue(provider.Store.TemplatedParentProperty, DependencyPropertyValuePrecedences.Local);
			}

			UpdateDataContext();
			UpdateTemplatedParent();

			if (oldChild is FrameworkElement ocfe)
			{
				ocfe.PointerPressed -= HandlePointerEvent;
				ocfe.PointerReleased -= HandlePointerEvent;
			}

			if (newChild is FrameworkElement ncfe)
			{
				ncfe.PointerPressed += HandlePointerEvent;
				ncfe.PointerReleased += HandlePointerEvent;
			}
		}

		private void HandlePointerEvent(object sender, PointerRoutedEventArgs e)
		{
			e.Handled = true;
		}

		protected internal override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			UpdateDataContext();
		}

		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			UpdateTemplatedParent();
		}

		private void UpdateDataContext()
		{
			_childHasOwnDataContext = false;
			if (Child is IDependencyObjectStoreProvider provider)
			{
				var dataContextProperty = provider.Store.ReadLocalValue(provider.Store.DataContextProperty);
				if (dataContextProperty != null && dataContextProperty != DependencyProperty.UnsetValue)
				{
					// Child already has locally set DataContext, we shouldn't overwrite it.
					_childHasOwnDataContext = true;
				}
				else
				{
					provider.Store.SetValue(provider.Store.DataContextProperty, this.DataContext, DependencyPropertyValuePrecedences.Local);
				}
			}
		}

		private void UpdateTemplatedParent()
		{
			if (Child is IDependencyObjectStoreProvider provider)
			{
				provider.Store.SetValue(provider.Store.TemplatedParentProperty, this.TemplatedParent, DependencyPropertyValuePrecedences.Local);
			}
		}

		/// <summary>
		/// A layouter responsible to layout the content of a popup at the right place
		/// </summary>
		internal interface IDynamicPopupLayouter
		{
			/// <summary>
			/// Measure the content of the popup
			/// </summary>
			/// <param name="available">The available size to place to render the popup. This is expected to be the screen size.</param>
			/// <param name="visibleSize">The size of the visible bounds of the window. This is expected to be AtMost the available.</param>
			/// <returns>The desired size to render the content</returns>
			Size Measure(Size available, Size visibleSize);

			/// <summary>
			/// Render the content of the popup at its final location
			/// </summary>
			/// <param name="finalSize">The final size available to render the view. This is expected to be the screen size.</param>
			/// <param name="visibleBounds">The frame of the visible bounds of the window. This is expected to be AtMost the finalSize.</param>
			/// <param name="desiredSize">The size at which the content expect to be rendered. This is the result of the last <see cref="Measure"/>.</param>
			/// <param name="upperLeftOffset">Coordinate system adjustment, applied to the resulting frame computed from the popup content</param>
			void Arrange(Size finalSize, Rect visibleBounds, Size desiredSize, Point? upperLeftOffset = null);
		}

		partial void OnIsLightDismissEnabledChangedPartial(bool oldIsLightDismissEnabled, bool newIsLightDismissEnabled)
		{
		}
	}
}
