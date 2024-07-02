#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Windows.System;

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

#if HAS_UNO_WINUI
using WindowSizeChangedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.WindowSizeChangedEventArgs;
#else
using WindowSizeChangedEventArgs = Windows.UI.Core.WindowSizeChangedEventArgs;
#endif

namespace Windows.UI.Xaml.Controls
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
		private TextBox? _editableText;
		private ContentPresenter? _headerContentPresenter;

		private DateTime m_timeSinceLastCharacterReceived;
		private bool m_isInSearchingMode;
		private string m_searchString = "";
		private bool m_searchResultIndexSet;
		private int m_searchResultIndex = -1;
		private int m_indexForcedToUnselectedVisual = -1;
		private int m_indexForcedToSelectedVisual = -1;

		private bool _wasPointerPressed;

		/// <summary>
		/// The 'inline' parent view of the selected item within the dropdown list. This is only set if SelectedItem is a view type.
		/// </summary>
		private ManagedWeakReference? _selectionParentInDropdown;

		public ComboBox()
		{
			ResourceResolver.ApplyResource(this, LightDismissOverlayBackgroundProperty, "ComboBoxLightDismissOverlayBackground", isThemeResourceExtension: true, isHotReloadSupported: true);

			DefaultStyleKey = typeof(ComboBox);
		}

		internal bool IsSearchResultIndexSet()
		{
			return m_searchResultIndexSet;
		}

		internal int GetSearchResultIndex()
		{
			return m_searchResultIndex;
		}

		public global::Windows.UI.Xaml.Controls.Primitives.ComboBoxTemplateSettings TemplateSettings { get; } = new Primitives.ComboBoxTemplateSettings();

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

			if (IsEditable)
			{
				_editableText = this.GetTemplateChild("EditableText") as TextBox;
			}

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

			if (XamlRoot is not null)
			{
				XamlRoot.Changed -= OnXamlRootChanged;
			}
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

		public static DependencyProperty IsTextSearchEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTextSearchEnabled),
				typeof(bool),
				typeof(ComboBox),
				new FrameworkPropertyMetadata(true));

		public bool IsTextSearchEnabled
		{
			get => (bool)this.GetValue(IsTextSearchEnabledProperty);
			set => this.SetValue(IsTextSearchEnabledProperty, value);
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
			base.OnPointerExited(e);
			_wasPointerPressed = false;

			UpdateVisualState();
		}

		protected override void OnPointerCanceled(PointerRoutedEventArgs e)
		{
			base.OnPointerCanceled(e);
			_wasPointerPressed = false;

			UpdateVisualState();
		}

		protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
		{
			base.OnPointerCaptureLost(e);
			_wasPointerPressed = false;

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

			_wasPointerPressed = true;

			UpdateVisualState(true);
			// On UWP ComboBox does handle the pressed event ... but does not capture it!
			args.Handled = true;
		}

		protected override void OnPointerReleased(PointerRoutedEventArgs args)
		{
			base.OnPointerReleased(args);

			if (!_wasPointerPressed)
			{
				// The dropdown should open only if the pointer was pressed and released on the ComboBox.
				return;
			}

			_wasPointerPressed = false;

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

			// Temporary as Uno doesn't yet implement CharacterReceived event.
			UnoOnCharacterReceived(args);
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
					// If we got a space while in searching mode, we shouldn't close the dropdown.
					if (!(args.Key == VirtualKey.Space && IsInSearchingMode()) && SelectedIndex > -1)
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
				if (_popup is { } p)
				{
					p.IsOpen = false;
				}
				// Don't handle. Let VisualTree.RootElement deal with focus management
			}
			return false;
		}

		// Uno TODO: This should override OnCharacterReceived when it's implemented.
		private void UnoOnCharacterReceived(KeyRoutedEventArgs args)
		{
			if (IsTextSearchEnabled)
			{
				if (args.UnicodeKey is not { } keyCode)
				{
					return;
				}

				ProcessSearch(keyCode);
			}
		}

		private bool HasSearchStringTimedOut()
		{
			const int timeOutInMilliseconds = 1000;

			var now = DateTime.UtcNow;

			return (now - m_timeSinceLastCharacterReceived).TotalMilliseconds > timeOutInMilliseconds;
		}

		private void ProcessSearch(char keyCode)
		{
			int foundIndex = -1;

			if (IsEditable)
			{
				if (_editableText is null)
				{
					return;
				}

				string textBoxText = _editableText.Text;

				// Don't process search if new text is equal to previous searched text.
				if (textBoxText.Equals(m_searchString))
				{
					return;
				}

				if (textBoxText.Length != 0)
				{
					foundIndex = SearchItemSourceIndex(keyCode, false /*startSearchFromCurrentIndex*/, false /*searchExactMatch*/);
				}
				else
				{
					m_searchString = "";
				}

				SetSearchResultIndex(foundIndex);

				var selectionChangedTrigger = SelectionChangedTrigger;

				if (selectionChangedTrigger == ComboBoxSelectionChangedTrigger.Always && foundIndex > -1)
				{
					SelectedIndex = foundIndex;
				}

				bool isDropDownOpen = IsDropDownOpen;

				// Override selected visuals only if popup is open
				if (isDropDownOpen)
				{
					OverrideSelectedIndexForVisualStates(foundIndex);
				}

				if (foundIndex >= 0)
				{
					if (isDropDownOpen)
					{
						// UNO TODO:
						//ScrollIntoView(
						//	foundIndex,
						//	false /*isGroupItemIndex*/,
						//	false /*isHeader*/,
						//	false /*isFooter*/,
						//	false /*isFromPublicAPI*/,
						//	true  /*ensureContainerRealized*/,
						//	false /*animateIfBringIntoView*/,
						//	ScrollIntoViewAlignment.Default);
					}
				}
			}
			else
			{
				foundIndex = SearchItemSourceIndex(keyCode, true /*startSearchFromCurrentIndex*/, false /*searchExactMatch*/);
				if (foundIndex >= 0)
				{
					SelectedIndex = foundIndex;
				}
			}
		}

		private int SearchItemSourceIndex(char keyCode, bool startSearchFromCurrentIndex, bool searchExactMatch)
		{
			// Get all of the ComboBox items; we'll try to convert them to strings later.
			var itemsVector = Items as IList<object>;
			int itemCount = itemsVector?.Count ?? 0;

			int searchIndex = -1;

			bool newStringCreated = false;

			// Editable ComboBox uses the text in the TextBox to search for values, Non-Editable ComboBox appends received characters to the current search string
			if (IsEditable)
			{
				if (_editableText is not null)
				{
					m_searchString = _editableText.Text;
				}
			}
			else
			{
				newStringCreated = AppendCharToSearchString(keyCode);
			}

			if (startSearchFromCurrentIndex)
			{
				int currentSelectedIndex = SelectedIndex;
				searchIndex = currentSelectedIndex;

				if (newStringCreated)
				{
					// If we've created a new search string, then we shouldn't search at i, but rather at i+1.
					if (searchIndex < itemCount - 1)
					{
						// We have at least one more item after this one to start searching at.
						searchIndex++;
					}
					else
					{
						// We are at the end of the list. Loop the search.
						searchIndex = 0;
					}
				}
				else
				{
					// If we just appended to the search string, then ensure that the search index is valid (>= 0)
					searchIndex = (searchIndex >= 0) ? searchIndex : 0;
				}
			}
			else
			{
				searchIndex = 0;
			}

			global::System.Diagnostics.Debug.Assert(searchIndex >= 0);

			object item;
			string strItem;
			int foundIndex = -1;

			//EnsurePropertyPathListener();

			// Iterate through all of the items. Try to get a string out of the item; if it matches, break. If not, keep looking.
			// TODO: [https://task.ms/6720676] Use CoreDispatcher/BuildTree to slice TypeAhead search logic
			for (int i = 0; i < itemCount; i++)
			{
				item = itemsVector![searchIndex];

				if (item is not null)
				{
					strItem = TryGetStringValue(item/*, _propertyPathListener*/);

					if (strItem is null)
					{
						// We couldn't get the string representing this item; it doesn't make sense to continue searching because
						// we're probably not going to be able to get strings from more items in this collection.
						break;
					}

					// Trim leading spaces on the item before comparing.
					strItem = strItem.TrimStart(' ');

					// On Editable mode Backspace should only search for exact matches. This prevents auto-complete from stopping backspacing.
					if (searchExactMatch || IsEditable && keyCode == (char)8)
					{
						if (AreStringsEqual(strItem, m_searchString))
						{
							foundIndex = searchIndex;

							break;
						}
					}
					else if (StartsWithIgnoreLinguisticSemantics(strItem, m_searchString))
					{
						foundIndex = searchIndex;

						// If matching item was found auto-complete word.
						if (IsEditable)
						{
							UpdateEditableTextBox(item, true /*selectText*/, false /*selectAll*/);
						}

						break;
					}
				}

				searchIndex++;

				// If we've gotten to the end of the list, loop the search.
				if (searchIndex == itemCount)
				{
					searchIndex = 0;
				}
			}

			return foundIndex;
		}

		private bool IsInSearchingMode()
		{
			if (HasSearchStringTimedOut())
			{
				m_isInSearchingMode = false;
			}
			return IsTextSearchEnabled && m_isInSearchingMode;
		}

		private string TryGetStringValue(object @object/*, PropertyPathListener pathListener*/)
		{
			object spBoxedValue;
			object spObject = @object;

			if (spObject is ICustomPropertyProvider spObjectPropertyAccessor)
			{
				//if (pathListener != null)
				//{
				//	// Our caller has provided us with a PropertyPathListener. By setting the source of the listener, we can pull a value out.
				//	// This is our boxedValue, which we effectively ToString below.
				//	pathListener.SetSource(spObject));
				//	spBoxedValue = pathListener.GetValue();
				//}
				//else
				{
					// No PathListener specified, but this object implements
					// ICustomPropertyProvider. Call .ToString on the object:
					return spObjectPropertyAccessor.GetStringRepresentation();
				}
			}
			else
			{
				// Try to get the string value by unboxing the object itself.
				spBoxedValue = spObject;
			}

			if (spBoxedValue != null)
			{
				if (spBoxedValue is IStringable spStringable)
				{
					// We've set a BoxedValue. If it is castable to a string, try to ToString it.
					return spStringable.ToString();
				}
				else
				{
					return spBoxedValue.ToString()!;
					// We've set a BoxedValue, but we can't directly ToString it. Try to get a string out of it.
				}
			}
			else
			{
				// If we haven't found a BoxedObject and it's not Stringable, try one last time to get a string out.
				return @object.ToString()!;
			}
		}

		private bool AppendCharToSearchString(char ch)
		{
			var createdNewString = false;
			if (HasSearchStringTimedOut())
			{
				ResetSearchString();
				createdNewString = true;
			}

			m_timeSinceLastCharacterReceived = DateTime.UtcNow;

			const int maxNumCharacters = 256;

			// Only append a new character if we're less than the max string length.
			if (m_searchString.Length <= maxNumCharacters)
			{
				m_searchString += ch;
			}
			m_isInSearchingMode = true;

			return createdNewString;
		}

		private void ResetSearchString()
		{
			m_searchString = "";
		}

		private void SetSearchResultIndex(int index)
		{
			m_searchResultIndexSet = true;
			m_searchResultIndex = index;
		}

		private void OverrideSelectedIndexForVisualStates(int selectedIndexOverride)
		{
			//Debug.Assert(!CanSelectMultiple);

			ClearSelectedIndexOverrideForVisualStates();

			// We only need to override the selected visual if the specified item is not
			// also the selected item.
			int selectedIndex = SelectedIndex;
			if (selectedIndexOverride != selectedIndex)
			{
				DependencyObject container;
				ComboBoxItem? comboBoxItem;

				// Force the specified override  item to appear selected.
				if (selectedIndexOverride != -1)
				{
					container = ContainerFromIndex(selectedIndexOverride);
					comboBoxItem = container as ComboBoxItem;
					if (comboBoxItem is not null)
					{
						comboBoxItem.OverrideSelectedVisualState(true /* appearSelected */);
					}
				}

				m_indexForcedToSelectedVisual = selectedIndexOverride;

				if (selectedIndex != -1)
				{
					// Force the actual selected item to appear unselected.
					container = ContainerFromIndex(selectedIndex);
					comboBoxItem = container as ComboBoxItem;
					if (comboBoxItem is not null)
					{
						comboBoxItem.OverrideSelectedVisualState(false /* appearSelected */);
					}

					m_indexForcedToUnselectedVisual = selectedIndex;
				}
			}
		}

		private void ClearSelectedIndexOverrideForVisualStates()
		{
			//Debug.Assert(!CanSelectMultiple);

			DependencyObject container;
			ComboBoxItem? comboBoxItem;

			if (m_indexForcedToUnselectedVisual != -1)
			{
				container = ContainerFromIndex(m_indexForcedToUnselectedVisual);
				comboBoxItem = container as ComboBoxItem;
				if (comboBoxItem is not null)
				{
					comboBoxItem.ClearSelectedVisualState();
				}

				m_indexForcedToUnselectedVisual = -1;
			}

			if (m_indexForcedToSelectedVisual != -1)
			{
				container = ContainerFromIndex(m_indexForcedToSelectedVisual);
				comboBoxItem = container as ComboBoxItem;
				if (comboBoxItem is not null)
				{
					comboBoxItem.ClearSelectedVisualState();
				}

				m_indexForcedToSelectedVisual = -1;
			}
		}

		//private void EnsurePropertyPathListener()
		//{
		//	if (_propertyPathListener is null)
		//	{
		//		string strDisplayMemberPath = DisplayMemberPath;

		//		if (!string.IsNullOrEmpty(strDisplayMemberPath))
		//		{
		//			// If we don't have one cached, create the property path listener
		//			// If strDisplayMemberPath contains something (a path), then use that to inform our PropertyPathListener.
		//			var propertyPathParser = new PropertyPathParser();

		//			propertyPathParser.SetSource(strDisplayMemberPath, false);

		//			_propertyPathListener = new PropertyPathListener(null, propertyPathParser, , false /*fListenToChanges*/, false /*fUseWeakReferenceForSource*/);
		//		}
		//	}
		//}

		private bool AreStringsEqual(string str1, string str2)
		{
			return str1.Equals(str2, StringComparison.OrdinalIgnoreCase);
		}

		private bool StartsWithIgnoreLinguisticSemantics(string strSource, string strPrefix)
		{
			// The goal of this method is to return true if strPrefix is found at the start of strSource regardless of linguistic semantics.
			// For example, if we've got strSource = "wAsHINGton" and strPrefix = "Wa", we should return true from this method.
			// FindNLSStringEx will return a 0-based index into the source string if it's successful; it will return < 0 if it failed to find a match.
			// We pass in a number of flags to achieve this behavior:
			// FIND_STARTSWITH : Test to find out if the strPrefix value is the first value in the Source string.
			// NORM_IGNORECASE: Ignore case (broader than LINGUISTIC_IGNORECASE)
			// NORM_IGNOREKANATYPE: Do not differentiate between hiragana and katakana characters (corresponding chars compare as equal)
			// NORM_IGNOREWIDTH: Used in Japanese and Chinese scripts, this flag ignores the difference between half- and full-width characters
			// NORM_LINGUISTIC_CASING: Use linguistic rules for casing instead of file system rules
			// LINGUISTIC_IGNOREDIACRITIC: Ignore diacritics (Dotless Turkish i maps to dotted i).
			return strSource.StartsWith(strPrefix, StringComparison.InvariantCultureIgnoreCase);
		}

		private void UpdateEditableTextBox(object item, bool selectText, bool selectAll)
		{
			if (item is null)
			{
				return;
			}

			string strItem;

			//EnsurePropertyPathListener();
			strItem = TryGetStringValue(item/*, m_spPropertyPathListener.Get()*/);

			UpdateEditableTextBox(strItem, selectText, selectAll);
		}

		private void UpdateEditableTextBox(string str, bool selectText, bool selectAll)
		{
			if (str is null)
			{
				return;
			}

			if (_editableText is not null)
			{
				string textBoxText = _editableText.Text;

				if (AreStringsEqual(str, textBoxText))
				{
					return;
				}

				m_searchString = str;

				if (selectAll)
				{
					// Selects all the text.
					_editableText.Text = m_searchString;

					if (selectText)
					{
						_editableText.SelectAll();
					}
				}
				else
				{
					// Selects auto-completed text for quick replacement.
					int selectionStart = _editableText.SelectionStart;
					_editableText.Text = m_searchString;

					if (selectText)
					{
						_editableText.Select(selectionStart, str.Length - selectionStart);
					}
				}
			}
		}

		internal InputDeviceType GetInputDeviceTypeUsedToOpen()
		{
			//return m_inputDeviceTypeUsedToOpen;
			// UNO TODO:
			return InputDeviceType.None;
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
					// In full screen mode, we want the popup to stretch horizontally, so we set MinWidth and MaxWidth to the available width.
					// However, we don't want it to stretch vertically.
					// We want the height to exactly show all the items, i.e, combo.ActualHeight * combo.Items.Count. However, we want to limit that by
					// both MaxDropDownHeight and visible height (quite similar to non-fullscreen mode).
					// This also allows the child to set MinHeight and provide a VerticalAlignment
					var maxHeight = Math.Min(visibleSize.Height, Math.Min(combo.MaxDropDownHeight, combo.ActualHeight * combo.Items.Count));

					child.MinWidth = available.Width;
					child.MaxWidth = available.Width;
					child.MinHeight = combo.ActualHeight;
					child.MaxHeight = maxHeight;
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

				var unoPlacement = Uno.UI.Xaml.Controls.ComboBox.GetDropDownPreferredPlacement(combo);
				var winUIPlacement = popup.DesiredPlacement;
				if (unoPlacement == DropDownPlacement.Auto)
				{
					// If the Uno placement is Auto, we use the WinUI placement
					unoPlacement = winUIPlacement switch
					{
						PopupPlacementMode.Auto => DropDownPlacement.Auto,
						PopupPlacementMode.Bottom => DropDownPlacement.Below,
						PopupPlacementMode.Top => DropDownPlacement.Above,
						_ => DropDownPlacement.Centered
					};
				}

				var stickyThreshold = Math.Max(1, Math.Min(4, (itemsCount / 2) - 1));
				switch (unoPlacement)
				{
					case DropDownPlacement.Below:
						frame.Y = comboRect.Bottom;
						break;
					case DropDownPlacement.Auto when selectedIndex >= 0 && selectedIndex < stickyThreshold:
						frame.Y = comboRect.Top;
						break;
					case DropDownPlacement.Above:
						frame.Y = comboRect.Top - frame.Height;
						break;
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

				frame.X += popup.HorizontalOffset;
				frame.Y += popup.VerticalOffset;

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
				// TODO: Adjust for multiwindow #13827
				var isTranslucent = Window.CurrentSafe!.IsStatusBarTranslucent();
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
