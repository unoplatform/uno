using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Uno.Client;
using System.Collections;
using Uno.UI.Controls;
using Uno.Extensions;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Input;
using Uno.Logging;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Uno.UI;
using System.Linq;
#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	// Temporarily inheriting from ListViewBase instead of Selector to leverage existing selection and virtualization code
	public partial class ComboBox : ListViewBase // TODO: Selector
	{
		public event EventHandler<object> DropDownClosed;
		public event EventHandler<object> DropDownOpened;

		private IPopup _popup;
		private Border _popupBorder;
		private FrameworkElement _background;
		private ContentPresenter _contentPresenter;
		private ContentPresenter _headerContentPresenter;

		public ComboBox()
		{
			IsItemClickEnabled = true;
		}

		public global::Windows.UI.Xaml.Controls.Primitives.ComboBoxTemplateSettings TemplateSettings { get; } = new Primitives.ComboBoxTemplateSettings();

		protected override DependencyObject GetContainerForItemOverride() => new ComboBoxItem { IsGeneratedContainer = true };

		protected override bool IsItemItsOwnContainerOverride(object item) => item is ComboBoxItem;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_popup = this.GetTemplateChild("Popup") as IPopup;
			_popupBorder = this.GetTemplateChild("PopupBorder") as Border;
			_background = this.GetTemplateChild("Background") as FrameworkElement;
			_contentPresenter = this.GetTemplateChild("ContentPresenter") as ContentPresenter;

			if (_background == null)
			{
				this.Log().Warn("ComboBox.Template is missing a 'Background' element. To ensure proper dropdown popup positionning, make sure the ControlTemplate contains a FrameworkElement named 'Background'.");
			}

			UpdateHeaderVisibility();
			UpdateContentPresenter();
		}

#if __ANDROID__
		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			// For some reasons, PointerReleased is not raised unless PointerPressed is Handled.
			args.Handled = true;
		}
#endif

		protected override void OnLoaded()
		{
			base.OnLoaded();

			UpdateDropDownState();

			if (_popup != null)
			{
				_popup.Closed += OnPopupClosed;
				_popup.Opened += OnPopupOpened;
			}

			Xaml.Window.Current.SizeChanged += OnWindowSizeChanged;
		}

		protected override void OnUnloaded()
		{
			base.OnUnloaded();

			if (_popup != null)
			{
				_popup.Closed -= OnPopupClosed;
				_popup.Opened -= OnPopupOpened;
			}

			Xaml.Window.Current.SizeChanged -= OnWindowSizeChanged;
		}

		private void OnWindowSizeChanged(object sender, Core.WindowSizeChangedEventArgs e)
		{
			IsDropDownOpen = false;
		}

		private void OnPopupOpened(object sender, object e)
		{
			IsDropDownOpen = true;
		}

		private void OnPopupClosed(object sender, object e)
		{
			IsDropDownOpen = false;
		}

		protected override void OnHeaderChanged(object oldHeader, object newHeader)
		{
			base.OnHeaderChanged(oldHeader, newHeader);
			UpdateHeaderVisibility();
		}

		protected override void OnHeaderTemplateChanged(DataTemplate oldHeaderTemplate, DataTemplate newHeaderTemplate)
		{
			base.OnHeaderTemplateChanged(oldHeaderTemplate, newHeaderTemplate);
			UpdateHeaderVisibility();
		}

		private void UpdateHeaderVisibility()
		{
			var headerVisibility = (Header != null || HeaderTemplate != null)
					? Visibility.Visible
					: Visibility.Collapsed;

			if (headerVisibility == Visibility.Visible && _headerContentPresenter == null)
			{
				_headerContentPresenter = this.GetTemplateChild("HeaderContentPresenter") as ContentPresenter;
				if (_headerContentPresenter != null)
				{
					// On Windows, all interactions involving the HeaderContentPresenter don't seem to affect the ComboBox.
					// For example, hovering/pressing doesn't trigger the PointOver/Pressed visual states. Tapping on it doesn't open the drop down.
					// This is true even if the Background of the root of ComboBox's template (which contains the HeaderContentPresenter) is set. 
					// Interaction with any other part of the control (including the root) triggers the corresponding visual states and actions.
					// It doesn't seem like the HeaderContentPresenter consumes (Handled = true) events because they are properly routed to the ComboBox.

					// My guess is that ComboBox checks whether the OriginalSource of Pointer events is a child of HeaderContentPresenter.

					// Because routed events are not implemented yet, the easy workaround is to prevent HeaderContentPresenter from being hit. 
					// This only works if the background of the root of ComboBox's template is null (which is the case by default).
					_headerContentPresenter.IsHitTestVisible = false;
				}
			}

			if (_headerContentPresenter != null)
			{
				_headerContentPresenter.Visibility = headerVisibility;
			}
		}

		internal override void OnSelectedItemChanged(object oldSelectedItem, object selectedItem)
		{
			base.OnSelectedItemChanged(oldSelectedItem, selectedItem);
			UpdateContentPresenter();
		}

		internal override void OnItemClicked(int clickedIndex)
		{
			base.OnItemClicked(clickedIndex);
			IsDropDownOpen = false;
		}

		private void UpdateContentPresenter()
		{
			if (_contentPresenter != null)
			{
				_contentPresenter.Content = SelectedItem;
			}
		}

		protected override void OnIsEnabledChanged(bool oldValue, bool newValue)
		{
			base.OnIsEnabledChanged(oldValue, newValue);

			UpdateCommonStates();

			OnIsEnabledChangedPartial(oldValue, newValue);
		}

		partial void OnIsEnabledChangedPartial(bool oldValue, bool newValue);

		partial void OnIsDropDownOpenChangedPartial(bool oldIsDropDownOpen, bool newIsDropDownOpen)
		{
			LayoutPopup();

			if (_popup != null)
			{
				_popup.IsOpen = newIsDropDownOpen;
			}

			if (newIsDropDownOpen)
			{
				DropDownOpened?.Invoke(this, newIsDropDownOpen);
			}
			else
			{
				DropDownClosed?.Invoke(this, newIsDropDownOpen);
			}

			UpdateDropDownState();
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			IsDropDownOpen = true;
		}

		// This is required by some apps trying to emulate the native iPhone look for ComboBox. 
		// The standard popup layouter works like on Windows, and doesn't stretch to take the full size of the screen.
		public bool IsPopupFullscreen { get; set; } = false;

		private void LayoutPopup()
		{
			if (IsDropDownOpen && _popup.Child is FrameworkElement popupChild)
			{
				// Because Popup.Child is not part of the visual tree until Popup.IsOpen,
				// some descendent Controls may never have loaded and materialized their templates.
				// We force the materialization of all templates to ensure that Measure works properly.
				foreach (var control in popupChild.EnumerateAllChildren().OfType<Control>())
				{
					control.ApplyTemplate();
				}

				if (_popup is Popup popup)
				{
					if (IsPopupFullscreen) // Legacy
					{
						// Location
						var popupOffset = popup.TransformToVisual(Xaml.Window.Current.Content) as TranslateTransform;
						popup.HorizontalOffset = -popupOffset.X;
						popup.VerticalOffset = -popupOffset.Y;
						// Size
						var windowSize = Xaml.Window.Current.Bounds.Size;
						popupChild.Width = windowSize.Width;
						popupChild.Height = windowSize.Height;
						return;
					}

					if (_background is FrameworkElement background)
					{
						// Reset popup offsets (Windows seems to do that)
						popup.VerticalOffset = 0;
						popup.HorizontalOffset = 0;

						// Inject layouting constraints
						popupChild.MinHeight = background.ActualHeight;
						popupChild.MinWidth = background.ActualWidth;
						popupChild.MaxHeight = MaxDropDownHeight;

						var windowRect = Xaml.Window.Current.Bounds;

						var popupTransform = popup.TransformToVisual(Xaml.Window.Current.Content) as TranslateTransform;
						var popupRect = new Rect(popupTransform.X, popupTransform.Y, popup.ActualWidth, popup.ActualHeight);

						var backgroundTransform = background.TransformToVisual(Xaml.Window.Current.Content) as TranslateTransform;
						var backgroundRect = new Rect(backgroundTransform.X, backgroundTransform.Y, background.ActualWidth, background.ActualHeight);

						// Because Popup.Child is not part of the visual tree until Popup.IsOpen,
						// some descendent Controls may never have loaded and materialized their templates.
						// We force the materialization of all templates to ensure that Measure works properly.
						foreach (var control in popupChild.EnumerateAllChildren().OfType<Control>())
						{
							control.ApplyTemplate();
						}

						popupChild.Measure(windowRect.Size);
						var popupChildRect = new Rect(new Point(), popupChild.DesiredSize);

						// Align left of popup with left of background 
						popupChildRect.X = backgroundRect.Left;
						if (popupChildRect.Right > windowRect.Right) // popup overflows at right
						{
							// Align right of popup with right of background
							popupChildRect.X = backgroundRect.Right - popupChildRect.Width;
						}
						if (popupChildRect.Left < windowRect.Left) // popup overflows at left
						{
							// Align center of popup with center of window
							popupChildRect.X = (windowRect.Width - popupChildRect.Width) / 2.0;
						}

						// Align top of popup with top of background
						popupChildRect.Y = backgroundRect.Top;
						if (popupChildRect.Bottom > windowRect.Bottom) // popup overflows at bottom
						{
							// Align bottom of popup with bottom of background
							popupChildRect.Y = backgroundRect.Bottom - popupChildRect.Height;
						}
						if (popupChildRect.Top < windowRect.Top) // popup overflows at top
						{
							// Align center of popup with center of window
							popupChildRect.Y = (windowRect.Height - popupChildRect.Height) / 2.0;
						}

						popup.HorizontalOffset = popupChildRect.X - popupRect.X;
						popup.VerticalOffset = popupChildRect.Y - popupRect.Y;
					}
				}
			}
		}

		private void UpdateDropDownState()
		{
			var state = IsDropDownOpen ? "Opened" : "Closed";
			VisualStateManager.GoToState(this, state, true);
		}

		private void UpdateCommonStates()
		{
			var state = IsEnabled ? "Normal" : "Disabled";
			VisualStateManager.GoToState(this, state, true);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ComboBoxAutomationPeer(this);
		}
	}
}
