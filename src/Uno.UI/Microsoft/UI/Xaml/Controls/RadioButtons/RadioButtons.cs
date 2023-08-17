#pragma warning disable 105 // remove when moving to WinUI tree

using System.Collections.Generic;
using Microsoft.UI.Private.Controls;
using Microsoft/* UWP don't rename */.UI.Xaml.Controls.Primitives;
using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Items))]
	public partial class RadioButtons : Control
	{
		public RadioButtons()
		{
			//__RP_Marker_ClassById(RuntimeProfiler.ProfId_RadioButtons);

			var items = new ObservableCollection<object>();
			SetValue(ItemsProperty, items);

			DefaultStyleKey = typeof(RadioButtons);

			// Override normal up/down/left/right behavior -- down should always go to the next item and up to the previous.
			// left and right should be spacial but contained to the RadioButtons control. We have to attach to PreviewKeyDown
			// because RadioButton has a key down handler for up and down that gets called before we can intercept. Issue #1634.
			var thisAsUIElement7 = this;
			if (thisAsUIElement7 != null)
			{
				thisAsUIElement7.PreviewKeyDown += OnChildPreviewKeyDown;
			}

			AccessKeyInvoked += OnAccessKeyInvoked;
			GettingFocus += OnGettingFocus;

			m_radioButtonsElementFactory = new RadioButtonsElementFactory();

			// RadioButtons adds handlers to its child radio button elements' checked and unchecked events.
			// To ensure proper lifetime management we create revokers for these elements and attach
			// the revokers to the child radio button via this attached property.  This way, if/when the child
			// is cleaned up we will automatically revoke the handler.
			// In Uno this is not needed, as we can just unsubscribe the events directly (see OnRepeaterElementClearing)
			//s_childHandlersProperty =
			//	InitializeDependencyProperty(
			//		s_childHandlersPropertyName,
			//		nameof(ChildHandlers),
			//		nameof(RadioButtons),
			//		true /* isAttached */,
			//		null,
			//		null);
		}

		protected override void OnApplyTemplate()
		{
			//IControlProtected controlProtected{ this };

			IsEnabledChanged += OnIsEnabledChanged;

			ItemsRepeater GetRepeater()
			{
				var repeater = GetTemplateChild<ItemsRepeater>(s_repeaterName);
				if (repeater != null)
				{
					repeater.ItemTemplate = m_radioButtonsElementFactory;

					repeater.ElementPrepared += OnRepeaterElementPrepared;
					repeater.ElementClearing += OnRepeaterElementClearing;
					repeater.ElementIndexChanged += OnRepeaterElementIndexChanged;
					repeater.Loaded += OnRepeaterLoaded;
					repeater.Unloaded += OnRepeaterUnloaded;
					return repeater;
				}
				return null;
			}
			m_repeater = GetRepeater();

			UpdateItemsSource();
			UpdateVisualStateForIsEnabledChange();
		}

		// When focus comes from outside the RadioButtons control we will put focus on the selected radio button.
		private void OnGettingFocus(object sender, GettingFocusEventArgs args)
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				var inputDevice = args.InputDevice;
				if (inputDevice == FocusInputDeviceKind.Keyboard)
				{
					// If focus is coming from outside the repeater, put focus on the selected item.
					var oldFocusedElement = args.OldFocusedElement;
					if (oldFocusedElement == null || repeater != VisualTreeHelper.GetParent(oldFocusedElement))
					{
						var selectedItem = repeater.TryGetElement(m_selectedIndex);
						if (selectedItem != null)
						{
							var argsAsIGettingFocusEventArgs2 = args as GettingFocusEventArgs;
							if (argsAsIGettingFocusEventArgs2 != null)
							{
								if (args.TrySetNewFocusedElement(selectedItem))
								{
									args.Handled = true;
								}
							}
						}
					}

					// Focus was already in the repeater: in On RS3+ Selection follows focus unless control is held down.
					else if (SharedHelpers.IsRS3OrHigher() &&
						(Windows.UI.Xaml.Window.IShouldntUseCurrentWindow.IShouldntUseCoreWindow.GetKeyState(VirtualKey.Control) &
							CoreVirtualKeyStates.Down) != CoreVirtualKeyStates.Down)
					{
						var newFocusedElementAsUIE = args.NewFocusedElement as UIElement;
						if (newFocusedElementAsUIE != null)
						{
							Select(repeater.GetElementIndex(newFocusedElementAsUIE));
							args.Handled = true;
						}
					}
				}
			}
		}

		private void OnRepeaterLoaded(object sender, RoutedEventArgs args)
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				if (m_testHooksEnabled)
				{
					AttachToLayoutChanged();
				}

				m_blockSelecting = false;
				if (SelectedIndex == -1 && SelectedItem != null)
				{
					UpdateSelectedItem();
				}
				else
				{
					UpdateSelectedIndex();
				}

				OnRepeaterCollectionChanged(null, null);
			}
		}

		private void OnChildPreviewKeyDown(object sender, KeyRoutedEventArgs args)
		{
			switch (args.Key)
			{
				case VirtualKey.Down:
					if (MoveFocusNext())
					{
						args.Handled = true;
						return;
					}
					else if (args.OriginalKey == VirtualKey.GamepadDPadDown)
					{
						if (FocusManager.TryMoveFocus(FocusNavigationDirection.Next))
						{
							args.Handled = true;
							return;
						}
					}
					args.Handled = HandleEdgeCaseFocus(false, args.OriginalSource);
					break;
				case VirtualKey.Up:
					if (MoveFocusPrevious())
					{
						args.Handled = true;
						return;
					}
					else if (args.OriginalKey == VirtualKey.GamepadDPadUp)
					{
						if (FocusManager.TryMoveFocus(FocusNavigationDirection.Previous))
						{
							args.Handled = true;
							return;
						}
					}
					args.Handled = HandleEdgeCaseFocus(true, args.OriginalSource);
					break;
				case VirtualKey.Right:
					if (args.OriginalKey != VirtualKey.GamepadDPadRight)
					{
						if (FocusManager.TryMoveFocus(FocusNavigationDirection.Right, GetFindNextElementOptions()))
						{
							args.Handled = true;
							return;
						}
					}
					else
					{
						if (FocusManager.TryMoveFocus(FocusNavigationDirection.Right))
						{
							args.Handled = true;
							return;
						}
					}
					args.Handled = HandleEdgeCaseFocus(false, args.OriginalSource);
					break;

				case VirtualKey.Left:
					if (args.OriginalKey != VirtualKey.GamepadDPadLeft)
					{
						if (FocusManager.TryMoveFocus(FocusNavigationDirection.Left, GetFindNextElementOptions()))
						{
							args.Handled = true;
							return;
						}
					}
					else
					{
						if (FocusManager.TryMoveFocus(FocusNavigationDirection.Left))
						{
							args.Handled = true;
							return;
						}
					}
					args.Handled = HandleEdgeCaseFocus(true, args.OriginalSource);
					break;
			}
		}

		private void OnAccessKeyInvoked(UIElement sender, AccessKeyInvokedEventArgs args)
		{
			// If RadioButtons is an AccessKeyScope then we do not want to handle the access
			// key invoked event because the user has (probably) set up access keys for the
			// RadioButton elements.
			if (!IsAccessKeyScope)
			{
				if (m_selectedIndex != 0)
				{
					var repeater = m_repeater;
					if (repeater != null)
					{
						var selectedItem = repeater.TryGetElement(m_selectedIndex);
						if (selectedItem != null)
						{
							var selectedItemAsControl = selectedItem as Control;
							if (selectedItemAsControl != null)
							{
								args.Handled = selectedItemAsControl.Focus(FocusState.Programmatic);
								return;
							}
						}
					}
				}
				// If we don't have a selected index, focus the RadioButton's which under normal
				// circumstances will put focus on the first radio button.
				args.Handled = this.Focus(FocusState.Programmatic);
			}
		}

		// If we haven't handled the key yet and the original source was the first(for up and left)
		// or last(for down and right) element in the repeater we need to handle the key so
		// RadioButton doesn't, which would result in the behavior.
		private bool HandleEdgeCaseFocus(bool first, object source)
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				var sourceAsUIElement = source as UIElement;
				if (sourceAsUIElement != null)
				{
					int GetIndex()
					{
						if (first)
						{
							return 0;
						}
						var itemsSourceView = repeater.ItemsSourceView;
						if (itemsSourceView != null)
						{
							return itemsSourceView.Count - 1;
						}
						return -1;
					}
					var index = GetIndex();

					if (repeater.GetElementIndex(sourceAsUIElement) == index)
					{
						return true;
					}
				}
			}
			return false;
		}

		private FindNextElementOptions GetFindNextElementOptions()
		{
			var findNextElementOptions = new FindNextElementOptions();
			findNextElementOptions.SearchRoot = this;
			return findNextElementOptions;
		}

		private void OnRepeaterElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
		{
			var element = args.Element;
			if (element != null)
			{
				var toggleButton = element as ToggleButton;
				if (toggleButton != null)
				{
					toggleButton.Checked += OnChildChecked;
					toggleButton.Unchecked += OnChildUnchecked;

					// If the developer adds a checked toggle button to the collection, update selection to this item.
					if (SharedHelpers.IsTrue(toggleButton.IsChecked))
					{
						Select(args.Index);
					}

					// TODO: Uno specific - remove when #4689 is fixed 
					//If SelectedItem/SelectedIndex has already been set by the time the elements are loaded, ensure we sync the selection
					if (args.Index == SelectedIndex)
					{
						toggleButton.IsChecked = true;
					}
				}
				var repeater = m_repeater;
				if (repeater != null)
				{
					var itemSourceView = repeater.ItemsSourceView;
					if (itemSourceView != null)
					{
						element.SetValue(AutomationProperties.PositionInSetProperty, args.Index + 1);
						element.SetValue(AutomationProperties.SizeOfSetProperty, itemSourceView.Count);
					}
				}
			}
		}

		private void OnRepeaterElementClearing(ItemsRepeater itemsRepeater, ItemsRepeaterElementClearingEventArgs args)
		{
			var element = args.Element;
			if (element != null)
			{
				// If the removed element was the selected one, update selection to -1
				var elementAsToggle = element as ToggleButton;
				if (elementAsToggle != null)
				{
					// Revoke
					elementAsToggle.Checked -= OnChildChecked;
					elementAsToggle.Unchecked -= OnChildUnchecked;

					if (SharedHelpers.IsTrue(elementAsToggle.IsChecked))
					{
						Select(-1);
					}
				}
			}
		}

		private void OnRepeaterElementIndexChanged(ItemsRepeater sender, ItemsRepeaterElementIndexChangedEventArgs args)
		{
			var element = args.Element;
			if (element != null)
			{
				element.SetValue(AutomationProperties.PositionInSetProperty, args.NewIndex + 1);

				// When the selected item's index changes, update selection to match
				var elementAsToggle = element as ToggleButton;
				if (elementAsToggle != null)
				{
					if (SharedHelpers.IsTrue(elementAsToggle.IsChecked))
					{
						Select(args.NewIndex);
					}
				}
			}
		}

		private void OnRepeaterCollectionChanged(object sender, object args)
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				var itemSourceView = repeater.ItemsSourceView;
				if (itemSourceView != null)
				{
					var count = itemSourceView.Count;
					for (var index = 0; index < count; index++)
					{
						var element = repeater.TryGetElement(index);
						if (element != null)
						{
							element.SetValue(AutomationProperties.SizeOfSetProperty, count);
						}
					}
				}
			}
		}

		private void Select(int index)
		{
			if (!m_blockSelecting && !m_currentlySelecting && m_selectedIndex != index)
			{
				// Calling Select updates the checked state on the radio button being selected
				// and the radio button being unselected, as well as updates the SelectedIndex
				// and SelectedItem DP. All of these things would cause Select to be called so
				// we'll prevent reentrency with this m_currentlySelecting boolean.
				try
				{
					m_currentlySelecting = true;

					var previousSelectedIndex = m_selectedIndex;
					m_selectedIndex = index;

					var newSelectedItem = GetDataAtIndex(m_selectedIndex, true);
					var previousSelectedItem = GetDataAtIndex(previousSelectedIndex, false);

					SelectedIndex = m_selectedIndex;
					SelectedItem = newSelectedItem;
					SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(new List<object>() { previousSelectedItem }, new List<object>() { newSelectedItem }));
					//TODO: MZ: What does this do when nothing was/is selected?
				}
				finally
				{
					m_currentlySelecting = false;
				}
			}
		}

		private object GetDataAtIndex(int index, bool containerIsChecked)
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				var item = repeater.TryGetElement(index);
				if (item != null)
				{
					var itemAsToggleButton = item as ToggleButton;
					if (itemAsToggleButton != null)
					{
						itemAsToggleButton.IsChecked = containerIsChecked;
					}
				}
				if (index >= 0)
				{
					var itemsSourceView = repeater.ItemsSourceView;
					if (itemsSourceView != null)
					{
						if (index < itemsSourceView.Count)
						{
							return itemsSourceView.GetAt(index);
						}
					}
				}
			}
			return (object)(null);
		}

		private void OnChildChecked(object sender, RoutedEventArgs args)
		{
			if (!m_currentlySelecting)
			{
				var repeater = m_repeater;
				if (repeater != null)
				{
					var senderAsUIE = sender as UIElement;
					if (senderAsUIE != null)
					{
						Select(repeater.GetElementIndex(senderAsUIE));
					}
				}
			}
		}

		private void OnChildUnchecked(object sender, RoutedEventArgs args)
		{
			if (!m_currentlySelecting)
			{
				var repeater = m_repeater;
				if (repeater != null)
				{
					var senderAsUIE = sender as UIElement;
					if (senderAsUIE != null)
					{
						if (m_selectedIndex == repeater.GetElementIndex(senderAsUIE))
						{
							Select(-1);
						}
					}
				}
			}
		}

		private bool MoveFocusNext()
		{
			return MoveFocus(1);
		}

		private bool MoveFocusPrevious()
		{
			return MoveFocus(-1);
		}

		private bool MoveFocus(int indexIncrement)
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				var focusedElement = FocusManager.GetFocusedElement(XamlRoot) as UIElement;
				if (focusedElement != null)
				{
					var focusedIndex = repeater.GetElementIndex(focusedElement);

					if (focusedIndex >= 0)
					{
						focusedIndex += indexIncrement;
						var itemCount = repeater.ItemsSourceView.Count;
						while (focusedIndex >= 0 && focusedIndex < itemCount)
						{
							var item = repeater.TryGetElement(focusedIndex);
							if (item != null)
							{
								var itemAsControl = item as Control;
								if (itemAsControl != null)
								{
									if (itemAsControl.Focus(FocusState.Programmatic))
									{
										return true;
									}
								}
							}
							focusedIndex += indexIncrement;
						}
					}
				}
			}
			return false;
		}

		private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
		{
			DependencyProperty property = args.Property;

			if (property == ItemsProperty || property == ItemsSourceProperty)
			{
				UpdateItemsSource();
			}
			else if (property == SelectedIndexProperty)
			{
				UpdateSelectedIndex();
			}
			else if (property == SelectedItemProperty)
			{
				UpdateSelectedItem();
			}
			else if (property == ItemTemplateProperty)
			{
				UpdateItemTemplate();
			}
		}

		private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			UpdateVisualStateForIsEnabledChange();
		}

		public UIElement ContainerFromIndex(int index)
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				return repeater.TryGetElement(index);
			}
			return null;
		}

		private void UpdateItemsSource()
		{
			Select(-1);

			var repeater = m_repeater;
			if (repeater != null)
			{
				// Revoke previous
				if (repeater.ItemsSourceView != null)
				{
					repeater.ItemsSourceView.CollectionChanged -= OnRepeaterCollectionChanged;
				}

				repeater.ItemsSource = GetItemsSource();

				var itemsSourceView = repeater.ItemsSourceView;
				if (itemsSourceView != null)
				{
					itemsSourceView.CollectionChanged += OnRepeaterCollectionChanged;
				}
			}
		}

		private object GetItemsSource()
		{
			var itemsSource = ItemsSource;
			if (itemsSource != null)
			{
				return itemsSource;
			}
			else
			{
				return Items;
			}
		}

		private void UpdateSelectedIndex()
		{
			if (!m_currentlySelecting)
			{
				Select(SelectedIndex);
			}
		}

		private void UpdateSelectedItem()
		{
			if (!m_currentlySelecting)
			{
				var repeater = m_repeater;
				if (repeater != null)
				{
					var itemsSourceView = repeater.ItemsSourceView;
					if (itemsSourceView != null)
					{
						Select(itemsSourceView.IndexOf(SelectedItem));
					}
				}
			}
		}

		private void UpdateItemTemplate()
		{
			m_radioButtonsElementFactory.UserElementFactory(ItemTemplate);
		}

		private void UpdateVisualStateForIsEnabledChange()
		{
			VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", false);
		}

		// Test Hooks helpers, only function when m_testHooksEnabled == true
		internal void SetTestHooksEnabled(bool enabled)
		{
			if (m_testHooksEnabled != enabled)
			{
				m_testHooksEnabled = enabled;
				if (enabled)
				{
					AttachToLayoutChanged();
				}
				else
				{
					DetatchFromLayoutChanged();
				}
			}
		}

		private void OnRepeaterUnloaded(object sender, RoutedEventArgs args)
		{
			var layout = GetLayout();
			if (layout != null)
			{
				layout.LayoutChanged -= OnLayoutChanged;
			}
		}

		private void OnLayoutChanged(ColumnMajorUniformToLargestGridLayout sender, object args)
		{
			RadioButtonsTestHooks.NotifyLayoutChanged(this);
		}

		internal int GetRows()
		{
			var layout = GetLayout();
			if (layout != null)
			{
				return layout.GetRows();
			}
			return -1;
		}

		internal int GetColumns()
		{
			var layout = GetLayout();
			if (layout != null)
			{
				return layout.GetColumns();
			}
			return -1;
		}

		internal int GetLargerColumns()
		{
			var layout = GetLayout();
			if (layout != null)
			{
				return layout.GetLargerColumns();
			}
			return -1;
		}


		private void AttachToLayoutChanged()
		{
			var layout = GetLayout();
			if (layout != null)
			{
				layout.SetTestHooksEnabled(true);
				layout.LayoutChanged += OnLayoutChanged;
			}
		}

		private void DetatchFromLayoutChanged()
		{
			var layout = GetLayout();
			if (layout != null)
			{
				layout.SetTestHooksEnabled(false);
				layout.LayoutChanged -= OnLayoutChanged;
			}
		}

		private ColumnMajorUniformToLargestGridLayout GetLayout()
		{
			var repeater = m_repeater;
			if (repeater != null)
			{
				var layout = repeater.Layout;
				if (layout != null)
				{
					var customLayout = layout as ColumnMajorUniformToLargestGridLayout;
					if (customLayout != null)
					{
						return customLayout;
					}
				}
			}
			return null;
		}
	}
}
