// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference RadioButtons.cpp

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Input;
using Microsoft.UI.Private.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.UI.Helpers.Boxes;
using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Items))]
	public partial class RadioButtons : Control
	{
		public RadioButtons()
		{
			//__RP_Marker_ClassById(RuntimeProfiler::ProfId_RadioButtons);

			var items = new ObservableCollection<object>();
			SetValue(ItemsProperty, items);

			this.SetDefaultStyleKey();

			// Override normal up/down/left/right behavior -- down should always go to the next item and up to the previous.
			// left and right should be spacial but contained to the RadioButtons control. We have to attach to PreviewKeyDown
			// because RadioButton has a key down handler for up and down that gets called before we can intercept. Issue #1634.
			PreviewKeyDown += OnChildPreviewKeyDown;

			AccessKeyInvoked += OnAccessKeyInvoked;
			GettingFocus += OnGettingFocus;

			m_radioButtonsElementFactory = new RadioButtonsElementFactory();

			// RadioButtons adds handlers to its child radio button elements' checked and unchecked events.
			// To ensure proper lifetime management we create revokers for these elements and attach
			// the revokers to the child radio button via this attached property.  This way, if/when the child
			// is cleaned up we will automatically revoke the handler.
#if !HAS_UNO // Uno specific: the handlers are removed directly in OnRepeaterElementClearing.
			s_childHandlersProperty =
				InitializeDependencyProperty(
					s_childHandlersPropertyName,
					nameof(ChildHandlers),
					nameof(RadioButtons),
					true /* isAttached */,
					null,
					null);
#endif
		}

		protected override AutomationPeer OnCreateAutomationPeer() => new RadioButtonsAutomationPeer(this);

		protected override void OnApplyTemplate()
		{
			IsEnabledChanged += OnIsEnabledChanged;
			m_isEnabledChangedRevoker.Disposable = Disposable.Create(() => IsEnabledChanged -= OnIsEnabledChanged);

			m_repeater = GetRepeater();

			ItemsRepeater GetRepeater()
			{
				if (GetTemplateChild(s_repeaterName) is ItemsRepeater repeater)
				{
					repeater.ItemTemplate = m_radioButtonsElementFactory;

					repeater.ElementPrepared += OnRepeaterElementPrepared;
					m_repeaterElementPreparedRevoker.Disposable = Disposable.Create(() => repeater.ElementPrepared -= OnRepeaterElementPrepared);
					repeater.ElementClearing += OnRepeaterElementClearing;
					m_repeaterElementClearingRevoker.Disposable = Disposable.Create(() => repeater.ElementClearing -= OnRepeaterElementClearing);
					repeater.ElementIndexChanged += OnRepeaterElementIndexChanged;
					m_repeaterElementIndexChangedRevoker.Disposable = Disposable.Create(() => repeater.ElementIndexChanged -= OnRepeaterElementIndexChanged);
					repeater.Loaded += OnRepeaterLoaded;
					m_repeaterLoadedRevoker.Disposable = Disposable.Create(() => repeater.Loaded -= OnRepeaterLoaded);
#if HAS_UNO
					repeater.Unloaded += OnRepeaterUnloaded;
#endif
					return repeater;
				}
				return null;
			}

			UpdateItemsSource();
			UpdateVisualStateForIsEnabledChange();
		}

		// When focus comes from outside the RadioButtons control we will put focus on the selected radio button.
		private void OnGettingFocus(object sender, GettingFocusEventArgs args)
		{
			if (m_repeater is { } repeater)
			{
				var inputDevice = args.InputDevice;
				if (inputDevice == FocusInputDeviceKind.Keyboard)
				{
					// If focus is coming from outside the repeater, put focus on the selected item.
					var oldFocusedElement = args.OldFocusedElement;
					if (oldFocusedElement is null || repeater != VisualTreeHelper.GetParent(oldFocusedElement))
					{
						if (repeater.TryGetElement(m_selectedIndex) is { } selectedItem)
						{
							if (args.TrySetNewFocusedElement(selectedItem))
							{
								args.Handled = true;
							}
						}
					}

					// Selection follows focus unless control is held down.
					else if ((InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control) & CoreVirtualKeyStates.Down) != CoreVirtualKeyStates.Down)
					{
						if (args.NewFocusedElement is UIElement newFocusedElementAsUIE)
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
			if (m_repeater is not null)
			{
				if (m_testHooksEnabled)
				{
					AttachToLayoutChanged();
				}

				m_blockSelecting = false;
				if (SelectedIndex == -1 && SelectedItem is not null)
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
						if (FocusManager.TryMoveFocus(FocusNavigationDirection.Next, GetFindNextElementOptions(false /*useThisAsSearchRoot*/)))
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
						if (FocusManager.TryMoveFocus(FocusNavigationDirection.Previous, GetFindNextElementOptions(false /*useThisAsSearchRoot*/)))
						{
							args.Handled = true;
							return;
						}
					}
					args.Handled = HandleEdgeCaseFocus(true, args.OriginalSource);
					break;
				case VirtualKey.Right:
					if (FocusManager.TryMoveFocus(FocusNavigationDirection.Right,
							GetFindNextElementOptions(args.OriginalKey != VirtualKey.GamepadDPadRight /*useThisAsSearchRoot*/)))
					{
						args.Handled = true;
						return;
					}
					args.Handled = HandleEdgeCaseFocus(false, args.OriginalSource);
					break;

				case VirtualKey.Left:
					if (FocusManager.TryMoveFocus(FocusNavigationDirection.Left,
						GetFindNextElementOptions(args.OriginalKey != VirtualKey.GamepadDPadLeft /*useThisAsSearchRoot*/)))
					{
						args.Handled = true;
						return;
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
					if (m_repeater is { } repeater)
					{
						if (repeater.TryGetElement(m_selectedIndex) is { } selectedItem)
						{
							if (selectedItem is Control selectedItemAsControl)
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
			if (m_repeater is { } repeater)
			{
				if (source is UIElement sourceAsUIElement)
				{
					int GetIndex()
					{
						if (first)
						{
							return 0;
						}
						if (repeater.ItemsSourceView is { } itemsSourceView)
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

		private FindNextElementOptions GetFindNextElementOptions(bool useThisAsSearchRoot)
		{
			var findNextElementOptions = new FindNextElementOptions();
			findNextElementOptions.SearchRoot = useThisAsSearchRoot ? this : XamlRoot.Content;
			return findNextElementOptions;
		}

		private void OnRepeaterElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
		{
			if (args.Element is { } element)
			{
				if (element is ToggleButton toggleButton)
				{
#if HAS_UNO
					// Uno specific: the handlers are removed in OnRepeaterElementClearing instead of by ChildHandlers revokers.
					toggleButton.Checked += OnChildChecked;
					toggleButton.Unchecked += OnChildUnchecked;
#else
					var childHandlers = new ChildHandlers();
					childHandlers.checkedRevoker = toggleButton.Checked(auto_revoke, OnChildChecked);
					childHandlers.uncheckedRevoker = toggleButton.Unchecked(auto_revoke, OnChildUnchecked);

					toggleButton.SetValue(s_childHandlersProperty, childHandlers);
#endif

					// If the developer adds a checked toggle button to the collection, update selection to this item.
					if (SharedHelpers.IsTrue(toggleButton.IsChecked))
					{
						Select(args.Index);
					}

#if HAS_UNO
					// TODO Uno: Elements are prepared after the repeater's Loaded (#4689), so OnRepeaterLoaded
					// cannot check the container of a SelectedIndex/SelectedItem set before load.
					if (args.Index == SelectedIndex)
					{
						toggleButton.IsChecked = true;
					}
#endif
				}
				if (m_repeater is { } repeater)
				{
					if (repeater.ItemsSourceView is { } itemSourceView)
					{
						element.SetValue(AutomationProperties.PositionInSetProperty, Boxer.Box(args.Index + 1));
						element.SetValue(AutomationProperties.SizeOfSetProperty, Boxer.Box(itemSourceView.Count));
					}
				}
			}
		}

		private void OnRepeaterElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
		{
			if (args.Element is { } element)
			{
#if HAS_UNO
				if (element is ToggleButton toggle)
				{
					toggle.Checked -= OnChildChecked;
					toggle.Unchecked -= OnChildUnchecked;
				}
#else
				element.SetValue(s_childHandlersProperty, null);
#endif

				// If the removed element was the selected one, update selection to -1
				if (element is ToggleButton elementAsToggle)
				{
					if (SharedHelpers.IsTrue(elementAsToggle.IsChecked))
					{
#if HAS_UNO
						// Uno specific: ItemsRepeater raises a fake Reset when it unloads (ItemsRepeater.OnUnloadedUno),
						// which clears the containers although the source is unchanged. Keep the selection then.
						if (IsLoaded && IsInLiveTree)
#endif
						{
							Select(-1);
						}
					}
				}
			}
		}

		private void OnRepeaterElementIndexChanged(ItemsRepeater sender, ItemsRepeaterElementIndexChangedEventArgs args)
		{
			if (args.Element is { } element)
			{
				element.SetValue(AutomationProperties.PositionInSetProperty, Boxer.Box(args.NewIndex + 1));

				// When the selected item's index changes, update selection to match
				if (element is ToggleButton elementAsToggle)
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
			if (m_repeater is { } repeater)
			{
				if (repeater.ItemsSourceView is { } itemSourceView)
				{
					var count = itemSourceView.Count;
					for (var index = 0; index < count; index++)
					{
						if (repeater.TryGetElement(index) is { } element)
						{
							element.SetValue(AutomationProperties.SizeOfSetProperty, Boxer.Box(count));
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
					SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(new List<object> { previousSelectedItem }, new List<object> { newSelectedItem }));
				}
				finally
				{
					m_currentlySelecting = false;
				}
			}
		}

		private object GetDataAtIndex(int index, bool containerIsChecked)
		{
			if (m_repeater is { } repeater)
			{
				if (repeater.TryGetElement(index) is { } item)
				{
					if (item is ToggleButton itemAsToggleButton)
					{
						itemAsToggleButton.IsChecked = containerIsChecked;
					}
				}
				if (index >= 0)
				{
					if (repeater.ItemsSourceView is { } itemsSourceView)
					{
						if (index < itemsSourceView.Count)
						{
							return itemsSourceView.GetAt(index);
						}
					}
				}
			}
			return null;
		}

		private void OnChildChecked(object sender, RoutedEventArgs args)
		{
			if (!m_currentlySelecting)
			{
				if (m_repeater is { } repeater)
				{
					if (sender is UIElement senderAsUIE)
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
				if (m_repeater is { } repeater)
				{
					if (sender is UIElement senderAsUIE)
					{
						if (m_selectedIndex == repeater.GetElementIndex(senderAsUIE))
						{
							Select(-1);
						}
					}
				}
			}
		}

		private bool MoveFocusNext() => MoveFocus(1);

		private bool MoveFocusPrevious() => MoveFocus(-1);

		private bool MoveFocus(int indexIncrement)
		{
			if (m_repeater is { } repeater)
			{
				if (FocusManager.GetFocusedElement(XamlRoot) is UIElement focusedElement)
				{
					var focusedIndex = repeater.GetElementIndex(focusedElement);

					if (focusedIndex >= 0)
					{
						focusedIndex += indexIncrement;
						var itemCount = repeater.ItemsSourceView.Count;
						while (focusedIndex >= 0 && focusedIndex < itemCount)
						{
							if (repeater.TryGetElement(focusedIndex) is { } item)
							{
								if (item.Focus(FocusState.Programmatic))
								{
									return true;
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

		private void OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs args) => UpdateVisualStateForIsEnabledChange();

		public UIElement ContainerFromIndex(int index)
		{
			if (m_repeater is { } repeater)
			{
				return repeater.TryGetElement(index);
			}
			return null;
		}

		private void UpdateItemsSource()
		{
			Select(-1);
			m_itemsSourceChanged.Disposable = null;
			if (m_repeater is { } repeater)
			{
				repeater.ItemsSource = GetItemsSource();

				if (repeater.ItemsSourceView is { } itemsSourceView)
				{
					itemsSourceView.CollectionChanged += OnRepeaterCollectionChanged;
					m_itemsSourceChanged.Disposable = Disposable.Create(() => itemsSourceView.CollectionChanged -= OnRepeaterCollectionChanged);
				}
			}
		}

		private object GetItemsSource()
		{
			if (ItemsSource is { } itemsSource)
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
				if (m_repeater is { } repeater)
				{
					if (repeater.ItemsSourceView is { } itemsSourceView)
					{
						Select(itemsSourceView.IndexOf(SelectedItem));
					}
				}
			}
		}

		private void UpdateItemTemplate() => m_radioButtonsElementFactory.UserElementFactory(ItemTemplate);

		private void UpdateVisualStateForIsEnabledChange() => VisualStateManager.GoToState(this, IsEnabled ? "Normal" : "Disabled", false);

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

#if !HAS_UNO // Uno specific: the layout belongs to this control's template, so its handler cannot keep the control alive.
		~RadioButtons()
		{
			if (m_layoutChangedToken)
			{
				if (GetLayout() is { } layout)
				{
					layout.LayoutChanged -= m_layoutChangedToken;
					m_layoutChangedToken = default;
				}
			}
		}
#endif

#if HAS_UNO
		// Uno specific: stands in for the destructor, detaching the test hooks' layout handler.
		private void OnRepeaterUnloaded(object sender, RoutedEventArgs args)
		{
			if (GetLayout() is { } layout)
			{
				layout.LayoutChanged -= OnLayoutChanged;
			}
		}
#endif

		private void OnLayoutChanged(ColumnMajorUniformToLargestGridLayout sender, object args) => RadioButtonsTestHooks.NotifyLayoutChanged(this);

		internal int GetRows()
		{
			if (GetLayout() is { } layout)
			{
				return layout.GetRows();
			}
			return -1;
		}

		internal int GetColumns()
		{
			if (GetLayout() is { } layout)
			{
				return layout.GetColumns();
			}
			return -1;
		}

		internal int GetLargerColumns()
		{
			if (GetLayout() is { } layout)
			{
				return layout.GetLargerColumns();
			}
			return -1;
		}

		private void AttachToLayoutChanged()
		{
			if (GetLayout() is { } layout)
			{
				layout.SetTestHooksEnabled(true);
				layout.LayoutChanged += OnLayoutChanged;
			}
		}

		private void DetatchFromLayoutChanged()
		{
			if (GetLayout() is { } layout)
			{
				layout.SetTestHooksEnabled(false);
				layout.LayoutChanged -= OnLayoutChanged;
			}
		}

		private ColumnMajorUniformToLargestGridLayout GetLayout()
		{
			if (m_repeater is { } repeater)
			{
				if (repeater.Layout is { } layout)
				{
					if (layout is ColumnMajorUniformToLargestGridLayout customLayout)
					{
						return customLayout;
					}
				}
			}
			return null;
		}
	}
}
