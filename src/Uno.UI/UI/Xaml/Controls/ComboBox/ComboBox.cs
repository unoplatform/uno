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
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Data;
using Microsoft.Extensions.Logging;

using Uno.UI.DataBinding;
using Uno.UI.Xaml.Controls;
using Windows.UI.Core;
#if __ANDROID__
using Android.Views;
using _View = Android.Views.View;
#elif __IOS__
using UIKit;
using _View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using _View = AppKit.NSView;
#else
using _View = Windows.UI.Xaml.FrameworkElement;
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
		private ContentPresenter _contentPresenter;
		private ContentPresenter _headerContentPresenter;

		/// <summary>
		/// The 'inline' parent view of the selected item within the dropdown list. This is only set if SelectedItem is a view type.
		/// </summary>
		private ManagedWeakReference _selectionParentInDropdown;

		public ComboBox()
		{
			ResourceResolver.ApplyResource(this, LightDismissOverlayBackgroundProperty, "ComboBoxLightDismissOverlayBackground", isThemeResourceExtension: true);

			IsItemClickEnabled = true;
			DefaultStyleKey = typeof(ComboBox);
		}

		public global::Windows.UI.Xaml.Controls.Primitives.ComboBoxTemplateSettings TemplateSettings { get; } = new Primitives.ComboBoxTemplateSettings();

		protected override DependencyObject GetContainerForItemOverride() => new ComboBoxItem { IsGeneratedContainer = true };

		protected override bool IsItemItsOwnContainerOverride(object item) => item is ComboBoxItem;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (_popup is PopupBase oldPopup)
			{
				oldPopup.CustomLayouter = null;
			}

			_popup = this.GetTemplateChild("Popup") as IPopup;
			_popupBorder = this.GetTemplateChild("PopupBorder") as Border;
			_contentPresenter = this.GetTemplateChild("ContentPresenter") as ContentPresenter;

			if (_popup is PopupBase popup)
			{
				popup.CustomLayouter = new DropDownLayouter(this, popup);

				popup.BindToEquivalentProperty(this, nameof(LightDismissOverlayMode));
				popup.BindToEquivalentProperty(this, nameof(LightDismissOverlayBackground));
			}

			UpdateHeaderVisibility();
			UpdateContentPresenter();

			if (_contentPresenter != null)
			{
				_contentPresenter.SetBinding(
					ContentPresenter.ContentTemplateProperty,
					new Binding(new PropertyPath("ItemTemplate"), null)
					{
						RelativeSource = RelativeSource.TemplatedParent
					});
				_contentPresenter.SetBinding(
					ContentPresenter.ContentTemplateSelectorProperty,
					new Binding(new PropertyPath("ItemTemplateSelector"), null)
					{
						RelativeSource = RelativeSource.TemplatedParent
					});

				_contentPresenter.DataContextChanged += (snd, evt) =>
				{
					// The ContentPresenter will automatically clear its local DataContext
					// on first load.
					//
					// When there's no selection, this will cause this ContentPresenter to
					// received the same DataContext as the ComboBox itself, which could
					// lead to strange result or errors.
					//
					// See comments in ContentPresenter.ResetDataContextOnFirstLoad() method.
					// Fixed in this PR: https://github.com/unoplatform/uno/pull/1465

					if (evt.NewValue != null && SelectedItem == null)
					{
						_contentPresenter.DataContext = null; // Remove problematic inherited DataContext
					}
				};

				UpdateCommonStates();
			}
		}

#if __ANDROID__
		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			// For some reasons, PointerReleased is not raised unless PointerPressed is Handled.
			args.Handled = true;
		}
#endif

		private protected override void OnLoaded()
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

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			if (_popup != null)
			{
				_popup.Closed -= OnPopupClosed;
				_popup.Opened -= OnPopupOpened;
			}

			Xaml.Window.Current.SizeChanged -= OnWindowSizeChanged;
		}

		private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e)
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

		internal override void OnSelectedItemChanged(object oldSelectedItem, object selectedItem, bool updateItemSelectedState)
		{
			if (oldSelectedItem is _View view)
			{
				// Ensure previous SelectedItem is put back in the dropdown list if it's a view
				RestoreSelectedItem(view);
			}

			base.OnSelectedItemChanged(
				oldSelectedItem: oldSelectedItem,
				selectedItem: selectedItem,
				updateItemSelectedState: updateItemSelectedState);

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
				var item = GetSelectionContent();

				var itemView = item as _View;

				if (itemView != null)
				{
#if __ANDROID__
					var comboBoxItem = itemView.FindFirstParentOfView<ComboBoxItem>();
#else
					var comboBoxItem = itemView.FindFirstParent<ComboBoxItem>();
#endif
					if (comboBoxItem != null)
					{
						// Keep track of the former parent, so we can put the item back when the dropdown is shown
						_selectionParentInDropdown = (itemView.GetVisualTreeParent() as IWeakReferenceProvider)?.WeakReference;
					}
				}
				else
				{
					_selectionParentInDropdown = null;
				}

				_contentPresenter.Content = item;

				if (itemView != null && itemView.GetVisualTreeParent() != _contentPresenter)
				{
					// Item may have been put back in list, reattach it to _contentPresenter
					_contentPresenter.AddChild(itemView);
				}
			}
		}

		private object GetSelectionContent()
		{
			return SelectedItem is ComboBoxItem cbi ? cbi.Content : SelectedItem;
		}

		private void RestoreSelectedItem()
		{
			var selection = GetSelectionContent();
			if (selection is _View selectionView)
			{
				RestoreSelectedItem(selectionView);
			}
		}

		/// <summary>
		/// Restore SelectedItem (or former SelectedItem) view to its position in the dropdown list.
		/// </summary>
		private void RestoreSelectedItem(_View selectionView)
		{
			var dropdownParent = _selectionParentInDropdown?.Target as FrameworkElement;
#if __ANDROID__
			var comboBoxItem = dropdownParent?.FindFirstParentOfView<ComboBoxItem>();
#else
			var comboBoxItem = dropdownParent?.FindFirstParent<ComboBoxItem>();
#endif

			// Sanity check, ensure parent is still valid (ComboBoxItem may have been recycled)
			if (comboBoxItem?.Content == selectionView && selectionView.GetVisualTreeParent() != dropdownParent)
			{
				dropdownParent.AddChild(selectionView);
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
			// This method will load the itempresenter children
#if __ANDROID__
			SetItemsPresenter((_popup.Child as ViewGroup).FindFirstChild<ItemsPresenter>());
#elif __IOS__ || __MACOS__
			SetItemsPresenter(_popup.Child.FindFirstChild<ItemsPresenter>());
#endif

			if (_popup != null)
			{
				_popup.IsOpen = newIsDropDownOpen;
			}

			if (newIsDropDownOpen)
			{
				DropDownOpened?.Invoke(this, newIsDropDownOpen);

				RestoreSelectedItem();

				if (SelectedItem != null)
				{
					ScrollIntoView(SelectedItem);
				}
			}
			else
			{
				DropDownClosed?.Invoke(this, newIsDropDownOpen);
				UpdateContentPresenter();
			}

			UpdateDropDownState();
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			IsDropDownOpen = true;
		}

		/// <summary>
		/// Stretches the opened Popup horizontally, and uses the VerticalAlignment
		/// of the first child for positioning.
		/// </summary>
		/// <remarks>
		/// This is required by some apps trying to emulate the native iPhone look for ComboBox.
		/// The standard popup layouter works like on Windows, and doesn't stretch to take the full size of the screen.
		/// </remarks>
		public bool IsPopupFullscreen { get; set; } = false;

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

		public LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}

		public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(LightDismissOverlayMode),
			typeof(ComboBox),
			new FrameworkPropertyMetadata(default(LightDismissOverlayMode)));

		/// <summary>
		/// Sets the light-dismiss colour, if the overlay is enabled. The external API for modifying this is to override the PopupLightDismissOverlayBackground, etc, static resource values.
		/// </summary>
		internal Brush LightDismissOverlayBackground
		{
			get { return (Brush)GetValue(LightDismissOverlayBackgroundProperty); }
			set { SetValue(LightDismissOverlayBackgroundProperty, value); }
		}

		internal static DependencyProperty LightDismissOverlayBackgroundProperty { get ; } =
			DependencyProperty.Register("LightDismissOverlayBackground", typeof(Brush), typeof(ComboBox), new FrameworkPropertyMetadata(null));

		private class DropDownLayouter : PopupBase.IDynamicPopupLayouter
		{
			private readonly ComboBox _combo;
			private readonly PopupBase _popup;

			public DropDownLayouter(ComboBox combo, PopupBase popup)
			{
				_combo = combo;
				_popup = popup;
			}

			/// <inheritdoc />
			public Size Measure(Size available, Size visibleSize)
			{
				if (!(_popup.Child is FrameworkElement child))
				{
					return new Size();
				}

				// Inject layouting constraints
				// Note: Even if this is ugly (as we should never alter properties of a random child like this),
				//		 it's how UWP behaves (but it does that only if the child is a Border, otherwise everything is messed up).
				//		 It sets (at least) those properties :
				//			MinWidth
				//			MinHeight
				//			MaxWidth
				//			MaxHeight

				if (_combo.IsPopupFullscreen)
				{
					// Size : Note we set both Min and Max to match the UWP behavior which alter only those
					//        properties. The MinHeight is not set to allow the the root child control to specificy
					//		  one and provide a VerticalAlignment.
					child.MinWidth = available.Width;
					child.MaxWidth = available.Width;
					child.MaxHeight = available.Height;
				}
				else
				{
					// Set the popup child as max 9 x the height of the combo
					// (UWP seams to actually limiting to 9 visible items ... which is not necessarily the 9 x the combo height)
					var maxHeight = Math.Min(visibleSize.Height, Math.Min(_combo.MaxDropDownHeight, _combo.ActualHeight * _itemsToShow));

					child.MinHeight = _combo.ActualHeight;
					child.MinWidth = _combo.ActualWidth;
					child.MaxHeight = maxHeight;
					child.MaxWidth = visibleSize.Width;
				}

				child.Measure(visibleSize);

				return child.DesiredSize;
			}

			private const int _itemsToShow = 9;

			/// <inheritdoc />
			public void Arrange(Size finalSize, Rect visibleBounds, Size desiredSize, Point? upperLeftLocation)
			{
				if (!(_popup.Child is FrameworkElement child))
				{
					return;
				}

				if (_combo.IsPopupFullscreen)
				{
					Point getChildLocation()
					{
						switch (child.VerticalAlignment)
						{
							default:
							case VerticalAlignment.Top:
								return new Point();
							case VerticalAlignment.Bottom:
								return new Point(0, finalSize.Height - child.DesiredSize.Height);
						}
					}

					var childSize = new Size(finalSize.Width, Math.Min(finalSize.Height, child.DesiredSize.Height));
					var finalRect = new Rect(getChildLocation(), childSize);

					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"FullScreen Layout for dropdown (desired: {desiredSize} / available: {finalSize} / visible: {visibleBounds} / finalRect: {finalRect} )");
					}

					child.Arrange(finalRect);

					return;
				}

				var comboRect = _combo.GetAbsoluteBoundsRect();
				var frame = new Rect(comboRect.Location, desiredSize.AtMost(visibleBounds.Size));

				// On windows, the popup is Y-aligned accordingly to the selected item in order to keep
				// the selected at the same place no matter if the drop down is open or not.
				// For instance if selected is:
				//  * the first option: The drop-down appears below the combobox
				//  * the last option: The dop-down appears above the combobox
				// However this would requires us to determine the actual location of the SelectedItem container's
				// which might not be ready at this point (we could try a 2-pass arrange), and to scroll into view to make it visible.
				// So for now we only rely on the SelectedIndex and make a highly improvable vertical alignment based on it.

				var itemsCount = _combo.NumberOfItems;
				var selectedIndex = _combo.SelectedIndex;
				if (selectedIndex < 0 && itemsCount > 0)
				{
					selectedIndex = itemsCount / 2;
				}

				var placement = Uno.UI.Xaml.Controls.ComboBox.GetDropDownPreferredPlacement(_combo);
				var stickyThreshold = Math.Max(1, Math.Min(4, (itemsCount / 2) - 1));
				switch (placement)
				{
					case DropDownPlacement.Below:
					case DropDownPlacement.Auto when selectedIndex >= 0 && selectedIndex < stickyThreshold:
						frame.Y = comboRect.Top;
						break;

					case DropDownPlacement.Above:
					case DropDownPlacement.Auto when
							selectedIndex >= 0 && selectedIndex >= itemsCount - stickyThreshold
							// As we don't scroll into view to the selected item, this case seems awkward if the selected item
							// is not directly visible (i.e. without scrolling) when the drop-down appears.
							// So if we detect that we should had to scroll to make it visible, we don't try to appear above!
							&& (itemsCount <= _itemsToShow && frame.Height < (_combo.ActualHeight * _itemsToShow) - 3):

						frame.Y = comboRect.Bottom - frame.Height;
						break;

					case DropDownPlacement.Centered:
					case DropDownPlacement.Auto: // For now we don't support other alignments than top/bottom/center, but on UWP auto can also be 2/3 - 1/3
					default:
						// Try to appear centered
						frame.Y = comboRect.Top - (frame.Height / 2.0) + (comboRect.Height / 2.0);
						break;
				}

				// Make sure that the popup does not appears out of the viewport
				if (frame.Left < visibleBounds.Left)
				{
					frame.X = visibleBounds.X;
				}
				else if (frame.Right > visibleBounds.Width)
				{
					// On UWP, the popup is just aligned to the right on the window if it overflows on right
					// Note: frame.Width is already at most visibleBounds.Width
					frame.X = visibleBounds.Width - frame.Width;
				}
				if (frame.Top < visibleBounds.Top)
				{
					frame.Y = visibleBounds.Y;
				}
				else if (frame.Bottom > visibleBounds.Height)
				{
					// On UWP, the popup always let 1 px free at the bottom
					// Note: frame.Height is already at most visibleBounds.Height
					frame.Y = visibleBounds.Height - frame.Height - 1;
				}

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Layout the combo's dropdown at {frame} (desired: {desiredSize} / available: {finalSize} / visible: {visibleBounds} / selected: {selectedIndex} of {itemsCount})");
				}

				if (upperLeftLocation is Point offset)
				{
					// Compensate for origin location is some popup providers (Android
					// is one, particularly when the status bar is translucent)
					frame.X -= offset.X;
					frame.Y -= offset.Y;
				}

				child.Arrange(frame);
			}
		}
	}
}
