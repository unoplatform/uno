#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;
using Uno.UI;
using Microsoft.UI.Xaml.Data;
using Windows.System;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

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
using _View = Microsoft.UI.Xaml.FrameworkElement;
#endif

#if HAS_UNO_WINUI
using WindowSizeChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.WindowSizeChangedEventArgs;
#else
using WindowSizeChangedEventArgs = Windows.UI.Core.WindowSizeChangedEventArgs;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ComboBox : Selector
	{
		public event EventHandler<object>? DropDownClosed;
		public event EventHandler<object>? DropDownOpened;

		private bool _areItemTemplatesForwarded;

		private IPopup? _popup;
		private Border? _popupBorder;
		private ContentPresenter? _contentPresenter;
		private TextBlock? _placeholderTextBlock;
		private ContentPresenter? _headerContentPresenter;

		/// <summary>
		/// The 'inline' parent view of the selected item within the dropdown list. This is only set if SelectedItem is a view type.
		/// </summary>
		private ManagedWeakReference? _selectionParentInDropdown;

		public ComboBox()
		{
			ResourceResolver.ApplyResource(this, LightDismissOverlayBackgroundProperty, "ComboBoxLightDismissOverlayBackground", isThemeResourceExtension: true, isHotReloadSupported: true);

			DefaultStyleKey = typeof(ComboBox);
		}

		public global::Microsoft.UI.Xaml.Controls.Primitives.ComboBoxTemplateSettings TemplateSettings { get; } = new Primitives.ComboBoxTemplateSettings();

		protected override DependencyObject GetContainerForItemOverride() => new ComboBoxItem { IsGeneratedContainer = true };

		protected override bool IsItemItsOwnContainerOverride(object item) => item is ComboBoxItem;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			if (_popup is Popup oldPopup)
			{
				oldPopup.CustomLayouter = null;
			}

			_popup = this.GetTemplateChild("Popup") as IPopup;
			_popupBorder = this.GetTemplateChild("PopupBorder") as Border;
			_contentPresenter = this.GetTemplateChild("ContentPresenter") as ContentPresenter;
			_placeholderTextBlock = this.GetTemplateChild("PlaceholderTextBlock") as TextBlock;

			if (_popup is Popup popup)
			{
				//TODO Uno specific: Ensures popup does not take focus when opened.
				//This can be removed when the actual ComboBox code is fully ported
				//from WinUI.
				if (_popupBorder is { } border)
				{
					border.AllowFocusOnInteraction = false;
				}

				popup.CustomLayouter = new DropDownLayouter(this, popup);

				popup.IsLightDismissEnabled = true;

				popup.BindToEquivalentProperty(this, nameof(LightDismissOverlayMode));
				popup.BindToEquivalentProperty(this, nameof(LightDismissOverlayBackground));
			}

			UpdateHeaderVisibility();
			UpdateContentPresenter();
			UpdateDescriptionVisibility(true);

			if (_contentPresenter != null)
			{
				_contentPresenter.SynchronizeContentWithOuterTemplatedParent = false;

				var thisRef = (this as IWeakReferenceProvider).WeakReference;
				_contentPresenter.DataContextChanged += (snd, evt) =>
				{
					if (thisRef.Target is ComboBox that)
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

						if (evt.NewValue != null && that.SelectedItem == null && that._contentPresenter != null)
						{
							that._contentPresenter.DataContext = null; // Remove problematic inherited DataContext
						}
					}
				};

				UpdateVisualState(true);
			}
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			UpdateDropDownState();

			if (_popup != null)
			{
				_popup.Closed += OnPopupClosed;
				_popup.Opened += OnPopupOpened;
			}

			if (XamlRoot is null)
			{
				throw new InvalidOperationException("XamlRoot must be set on Loaded");
			}

			XamlRoot.Changed += OnXamlRootChanged;
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			if (_popup != null)
			{
				_popup.Closed -= OnPopupClosed;
				_popup.Opened -= OnPopupOpened;
			}

			if (XamlRoot is null)
			{
				throw new InvalidOperationException("XamlRoot must be set on Loaded");
			}

			XamlRoot.Changed -= OnXamlRootChanged;
		}

		protected virtual void OnDropDownClosed(object e)
		{
			DropDownClosed?.Invoke(this, null!);
		}

		protected virtual void OnDropDownOpened(object e)
		{
			DropDownOpened?.Invoke(this, null!);
		}

		private void OnXamlRootChanged(object sender, XamlRootChangedEventArgs e)
		{
			IsDropDownOpen = false;
		}

		private void OnPopupOpened(object? sender, object e)
		{
			IsDropDownOpen = true;
		}

		private void OnPopupClosed(object? sender, object e)
		{
			IsDropDownOpen = false;
		}


		public object Header
		{
			get { return (object)this.GetValue(HeaderProperty); }
			set { this.SetValue(HeaderProperty, value); }
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				"Header",
				typeof(object),
				typeof(ComboBox),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.None,
					propertyChangedCallback: (s, e) => ((ComboBox)s)?.OnHeaderChanged((object)e.OldValue, (object)e.NewValue)
				)
			);

		private void OnHeaderChanged(object oldHeader, object newHeader)
		{
			UpdateHeaderVisibility();
		}


		public DataTemplate HeaderTemplate
		{
			get { return (DataTemplate)this.GetValue(HeaderTemplateProperty); }
			set { this.SetValue(HeaderTemplateProperty, value); }
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(
				"HeaderTemplate",
				typeof(DataTemplate),
				typeof(ComboBox),
				new FrameworkPropertyMetadata(
					defaultValue: (DataTemplate?)null,
					options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
					propertyChangedCallback: (s, e) => ((ComboBox)s)?.OnHeaderTemplateChanged((DataTemplate)e.OldValue, (DataTemplate)e.NewValue)
				)
			);

		private void OnHeaderTemplateChanged(DataTemplate oldHeaderTemplate, DataTemplate newHeaderTemplate)
		{
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

		public
#if __IOS__ || __MACOS__
		new
#endif
			object Description
		{
			get => this.GetValue(DescriptionProperty);
			set => this.SetValue(DescriptionProperty, value);
		}

		public static DependencyProperty DescriptionProperty { get; } =
			DependencyProperty.Register(
				nameof(Description), typeof(object),
				typeof(ComboBox),
				new FrameworkPropertyMetadata(default(object), propertyChangedCallback: (s, e) => (s as ComboBox)?.UpdateDescriptionVisibility(false)));

		private void UpdateDescriptionVisibility(bool initialization)
		{
			if (initialization && Description == null)
			{
				// Avoid loading DescriptionPresenter element in template if not needed.
				return;
			}

			var descriptionPresenter = this.FindName("DescriptionPresenter") as ContentPresenter;
			if (descriptionPresenter != null)
			{
				descriptionPresenter.Visibility = Description != null ? Visibility.Visible : Visibility.Collapsed;
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

			if (updateItemSelectedState)
			{
				TryUpdateSelectorItemIsSelected(oldSelectedItem, false);
				TryUpdateSelectorItemIsSelected(selectedItem, true);
			}
		}

		protected override void OnPointerEntered(PointerRoutedEventArgs e)
		{
			base.OnPointerEntered(e);

			UpdateVisualState();
		}

		protected override void OnPointerExited(PointerRoutedEventArgs e)
		{
			base.OnPointerEntered(e);

			UpdateVisualState();
		}

		protected override void OnPointerCanceled(PointerRoutedEventArgs e)
		{
			base.OnPointerCanceled(e);

			UpdateVisualState();
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
		{
			base.OnPointerCaptureLost(e);

			UpdateVisualState();
		}

		internal override void OnItemClicked(int clickedIndex, VirtualKeyModifiers modifiers)
		{
			base.OnItemClicked(clickedIndex, modifiers);
			IsDropDownOpen = false;
		}

		private void UpdateContentPresenter()
		{
			if (_contentPresenter == null) return;

			if (SelectedItem != null)
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

				var displayMemberPath = DisplayMemberPath;
				if (string.IsNullOrEmpty(displayMemberPath))
				{
					_contentPresenter.Content = item;
				}
				else
				{
					var b = new BindingPath(displayMemberPath, item) { DataContext = item };
					_contentPresenter.Content = b.Value;
				}

				if (itemView != null && itemView.GetVisualTreeParent() != _contentPresenter)
				{
					// Item may have been put back in list, reattach it to _contentPresenter
					_contentPresenter.AddChild(itemView);
				}
				if (!_areItemTemplatesForwarded)
				{
					SetContentPresenterBinding(ContentPresenter.ContentTemplateProperty, nameof(ItemTemplate));
					SetContentPresenterBinding(ContentPresenter.ContentTemplateSelectorProperty, nameof(ItemTemplateSelector));

					_areItemTemplatesForwarded = true;
				}
			}
			else
			{
				_contentPresenter.Content = _placeholderTextBlock;
				if (_areItemTemplatesForwarded)
				{
					_contentPresenter.ClearValue(ContentPresenter.ContentTemplateProperty);
					_contentPresenter.ClearValue(ContentPresenter.ContentTemplateSelectorProperty);

					_areItemTemplatesForwarded = false;
				}
			}

			void SetContentPresenterBinding(DependencyProperty targetProperty, string sourcePropertyPath)
			{
				_contentPresenter?.SetBinding(targetProperty, new Binding(sourcePropertyPath) { RelativeSource = RelativeSource.TemplatedParent });
			}
		}

		private object? GetSelectionContent()
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
			if (dropdownParent != null
				&& comboBoxItem?.Content == selectionView
				&& selectionView.GetVisualTreeParent() != dropdownParent)
			{
				dropdownParent.AddChild(selectionView);
			}
		}

		private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs e)
		{
			base.OnIsEnabledChanged(e);

			UpdateVisualState(true);
		}

		partial void OnIsDropDownOpenChangedPartial(bool oldIsDropDownOpen, bool newIsDropDownOpen)
		{
			if (_popup != null)
			{
				// This method will load the itempresenter children
#if __ANDROID__
				SetItemsPresenter((_popup.Child as ViewGroup).FindFirstChild<ItemsPresenter>());
#elif __IOS__ || __MACOS__
				SetItemsPresenter(_popup.Child.FindFirstChild<ItemsPresenter>());
#endif

				_popup.IsOpen = newIsDropDownOpen;
			}

			var args = new RoutedEventArgs() { OriginalSource = this };
			if (newIsDropDownOpen)
			{
				// Force a refresh of the popup's ItemPresenter
				Refresh();

				OnDropDownOpened(args);

				RestoreSelectedItem();

				var index = SelectedIndex;
				index = index == -1 ? 0 : index;
				if (ContainerFromIndex(index) is ComboBoxItem container)
				{
					container.Focus(FocusState.Programmatic);
				}
			}
			else
			{
				OnDropDownClosed(args);
				UpdateContentPresenter();
			}

			UpdateDropDownState();
			ChangeVisualState(true);
		}

		protected override void OnPointerPressed(PointerRoutedEventArgs args)
		{
			base.OnPointerPressed(args);

			UpdateVisualState(true);
			// On UWP ComboBox does handle the pressed event ... but does not capture it!
			args.Handled = true;
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);

			Focus(FocusState.Programmatic);
			IsDropDownOpen = true;

			UpdateVisualState(true);

			// On UWP ComboBox does handle the released event.
			args.Handled = true;
		}

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			base.OnKeyDown(args);

			if (!args.Handled)
			{
				args.Handled = TryHandleKeyDown(args, null);
			}
		}

		internal bool TryHandleKeyDown(KeyRoutedEventArgs args, ComboBoxItem? focusedContainer)
		{
			if (!IsEnabled)
			{
				return false;
			}

			if (args.Key == VirtualKey.Enter ||
				args.Key == VirtualKey.Space)
			{
				if (IsDropDownOpen)
				{
					if (SelectedIndex > -1)
					{
						IsDropDownOpen = false;
						return true;
					}
				}
				else
				{
					IsDropDownOpen = true;
					return true;
				}
			}
			else if (args.Key == VirtualKey.Escape)
			{
				if (IsDropDownOpen)
				{
					IsDropDownOpen = false;
					return true;
				}
			}
			else if (args.Key == VirtualKey.Down)
			{
				if (IsDropDownOpen)
				{
					return TryMoveKeyboardFocus(+1, focusedContainer);
				}
				else
				{
					if (IsIndexValid(SelectedIndex + 1))
					{
						SelectedIndex = SelectedIndex + 1;
						return true;
					}
				}
			}
			else if (args.Key == VirtualKey.Up)
			{
				if (IsDropDownOpen)
				{
					return TryMoveKeyboardFocus(-1, focusedContainer);
				}
				else
				{
					if (IsIndexValid(SelectedIndex - 1))
					{
						SelectedIndex = SelectedIndex - 1;
						return true;
					}
				}
			}
			else if (args.Key == VirtualKey.Tab)
			{
				var dropDownWasOpen = IsDropDownOpen;
				if (_popup is { } p)
				{
					p.IsOpen = false;
				}
				// Don't handle. Let VisualTree.RootElement deal with focus management

				if (dropDownWasOpen)
				{
					var focusManager = VisualTree.GetFocusManagerForElement(this);

					// Set the focus on the next focusable element if Tab was pressed while the Popup is open.
					// In this case, we got here through ComboBoxItem which is inside the Popup.
					// Focus management would normally be dealt with at the VisualTree.RootElement level (UnoFocusInputManager), but because
					// the Popup collapsed, the PopupPanel was removed from the visual tree and event propagation won't go up that far.
					// Alternatively, we handle the focus here.
					focusManager?.TryMoveFocusInstance(
						args.KeyboardModifiers.HasFlag(VirtualKeyModifiers.Shift) ?
						FocusNavigationDirection.Previous :
						FocusNavigationDirection.Next
					);
				}
			}
			return false;
		}

		private bool TryMoveKeyboardFocus(int offset, ComboBoxItem? focusedContainer)
		{
			var focusedIndex = SelectedIndex;
			if (focusedContainer != null)
			{
				focusedIndex = IndexFromContainer(focusedContainer);
			}

			var index = focusedIndex + offset;
			if (!IsIndexValid(index))
			{
				return false;
			}

			var container = ContainerFromIndex(index);
			if (container is not ComboBoxItem item)
			{
				return false;
			}

			item.StartBringIntoView(new BringIntoViewOptions()
			{
				AnimationDesired = false
			});
			item.Focus(FocusState.Keyboard);
			return true;
		}

		private bool IsIndexValid(int index) => index >= 0 && index < NumberOfItems;

		/// <summary>
		/// Stretches the opened Popup horizontally, and uses the VerticalAlignment
		/// of the first child for positioning.
		/// </summary>
		/// <remarks>
		/// This is required by some apps trying to emulate the native iPhone look for ComboBox.
		/// The standard popup layouter works like on Windows, and doesn't stretch to take the full size of the screen.
		/// </remarks>
		public bool IsPopupFullscreen { get; set; }

		private void UpdateDropDownState()
		{
			var state = IsDropDownOpen ? "Opened" : "Closed";
			VisualStateManager.GoToState(this, state, true);
		}

		protected override AutomationPeer OnCreateAutomationPeer()
		{
			return new ComboBoxAutomationPeer(this);
		}

		protected override void OnGotFocus(RoutedEventArgs e) => UpdateVisualState();

		protected override void OnLostFocus(RoutedEventArgs e) => UpdateVisualState();

		private protected override void ChangeVisualState(bool useTransitions)
		{
			if (!IsEnabled)
			{
				GoToState(useTransitions, "Disabled");
			}
			else if (IsDropDownOpen)
			{
				GoToState(useTransitions, "Highlighted");
			}
			else if (IsPointerPressed)
			{
				GoToState(useTransitions, "Pressed");
			}
			else if (IsPointerOver)
			{
				GoToState(useTransitions, "PointerOver");
			}
			else
			{
				GoToState(useTransitions, "Normal");
			}

			// FocusStates VisualStateGroup.
			if (!IsEnabled)
			{
				GoToState(useTransitions, "Unfocused");
			}
			else if (IsDropDownOpen)
			{
				GoToState(useTransitions, "FocusedDropDown");
			}
			else
			{
				var focusVisualState = FocusState switch
				{
					FocusState.Unfocused => "Unfocused",
					FocusState.Pointer => "PointerFocused",
					_ => IsPointerPressed ? "FocusedPressed" : "Focused",
				};

				GoToState(useTransitions, focusVisualState);
			}
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

		internal static DependencyProperty LightDismissOverlayBackgroundProperty { get; } =
			DependencyProperty.Register("LightDismissOverlayBackground", typeof(Brush), typeof(ComboBox), new FrameworkPropertyMetadata(null));

		private class DropDownLayouter : Popup.IDynamicPopupLayouter
		{
			private ManagedWeakReference _combo;
			private ManagedWeakReference _popup;

			private ComboBox? Combo => _combo.Target as ComboBox;
			private Popup? Popup => _popup.Target as Popup;

			public DropDownLayouter(ComboBox combo, Popup popup)
			{
				_combo = (combo as IWeakReferenceProvider).WeakReference;
				_popup = (popup as IWeakReferenceProvider).WeakReference;
			}

			/// <inheritdoc />
			public Size Measure(Size available, Size visibleSize)
			{
				var popup = Popup;
				var combo = Combo;

				if (!(popup?.Child is FrameworkElement child) || combo == null)
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

				if (combo.IsPopupFullscreen)
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
					var maxHeight = Math.Min(visibleSize.Height, Math.Min(combo.MaxDropDownHeight, combo.ActualHeight * _itemsToShow));

					child.MinHeight = combo.ActualHeight;
					child.MinWidth = combo.ActualWidth;
					child.MaxHeight = maxHeight;
					child.MaxWidth = visibleSize.Width;

					if (UsesManagedLayouting)
					// This is a breaking change for Android/iOS in some specialized cases (see ComboBox_VisibleBounds sample), and
					// since the layouting on those platforms is not yet as aligned with UWP as on WASM/Skia, and in particular
					// virtualizing panels aren't used in the ComboBox yet (#556 and #1133), we skip it for now
					{
#pragma warning disable CS0162 // Unreachable code detected
						child.HorizontalAlignment = HorizontalAlignment.Left;
						child.VerticalAlignment = VerticalAlignment.Top;
#pragma warning restore CS0162 // Unreachable code detected
					}
				}

				child.Measure(visibleSize);

				return child.DesiredSize;
			}

			private const int _itemsToShow = 9;

			/// <inheritdoc />
			public void Arrange(Size finalSize, Rect visibleBounds, Size desiredSize)
			{
				var popup = Popup;
				var combo = Combo;

				if (!(popup?.Child is FrameworkElement child) || combo == null)
				{
					return;
				}

				if (combo.IsPopupFullscreen)
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

				var comboRect = combo.GetAbsoluteBoundsRect();
				var frame = new Rect(comboRect.Location, desiredSize.AtMost(visibleBounds.Size));

				// On windows, the popup is Y-aligned accordingly to the selected item in order to keep
				// the selected at the same place no matter if the drop down is open or not.
				// For instance if selected is:
				//  * the first option: The drop-down appears below the combobox
				//  * the last option: The dop-down appears above the combobox
				// However this would requires us to determine the actual location of the SelectedItem container's
				// which might not be ready at this point (we could try a 2-pass arrange), and to scroll into view to make it visible.
				// So for now we only rely on the SelectedIndex and make a highly improvable vertical alignment based on it.

				var itemsCount = combo.NumberOfItems;
				var selectedIndex = combo.SelectedIndex;
				if (selectedIndex < 0 && itemsCount > 0)
				{
					selectedIndex = itemsCount / 2;
				}

				var placement = Uno.UI.Xaml.Controls.ComboBox.GetDropDownPreferredPlacement(combo);
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
							&& (itemsCount <= _itemsToShow && frame.Height < (combo.ActualHeight * _itemsToShow) - 3):

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

#if __ANDROID__
				// Check whether the status bar is translucent
				// If so, we may need to compensate for the origin location
				var isTranslucent = Window.Current.IsStatusBarTranslucent();
				var allowUnderStatusBar = FeatureConfiguration.ComboBox.AllowPopupUnderTranslucentStatusBar;
				if (isTranslucent && allowUnderStatusBar)
				{
					var offset = visibleBounds.Location;
					frame.X -= offset.X;
					frame.Y -= offset.Y;
				}
#endif

				child.Arrange(frame);
			}
		}
	}
}
