// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.System;
using Windows.UI.Xaml.Input;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;
using Uno.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarView
	{

		// Handles when a key is pressed down on the CalendarView.
		protected override void OnKeyDown( // TODO UNO: Does this should really override the method from Control, or just list for event on ScrollViewers ?
			KeyRoutedEventArgs pArgs)
		{
			bool isHandled = false;
			bool handled = false;
			KeyRoutedEventArgs spKeyDownArgsFromCalendarItem;

			VirtualKey key = VirtualKey.None;
			VirtualKeyModifiers modifiers = VirtualKeyModifiers.None;

			base.OnKeyDown(pArgs);

			// Check if this keydown events comes from CalendarItem.
			m_wrKeyDownEventArgsFromCalendarItem.TryGetTarget(out spKeyDownArgsFromCalendarItem);

			if (spKeyDownArgsFromCalendarItem is null)
			{
				return;
			}

			if (spKeyDownArgsFromCalendarItem != pArgs)
			{
				m_wrKeyDownEventArgsFromCalendarItem.SetTarget(null);
				return;
			}

			// Ignore already handled events
			isHandled = pArgs.Handled;
			if (isHandled)
			{
				return;
			}

			modifiers = pArgs.KeyboardModifiers;
			key = pArgs.Key;

			if (key == VirtualKey.GamepadLeftTrigger)
			{
				key = VirtualKey.PageUp;
			}

			if (key == VirtualKey.GamepadRightTrigger)
			{
				key = VirtualKey.PageDown;
			}

			// navigation keys without modifier . do keyboard navigation.
			if (modifiers == VirtualKeyModifiers.None)
			{
				switch (key)
				{
					case VirtualKey.Home:
					case VirtualKey.End:
					case VirtualKey.PageUp:
					case VirtualKey.PageDown:
					case VirtualKey.Left:
					case VirtualKey.Right:
					case VirtualKey.Up:
					case VirtualKey.Down:
						{
							VirtualKey originalKey = VirtualKey.None;
							originalKey = pArgs.OriginalKey;

							handled = OnKeyboardNavigation(key, originalKey);
							break;
						}
					default:
						break;
				}
			}
			// Ctrl+Up/Down . switch display modes.
			else if (modifiers == VirtualKeyModifiers.Control)
			{
				switch (key)
				{
					case VirtualKey.Up:
					case VirtualKey.Down:
						{
							CalendarViewDisplayMode mode = CalendarViewDisplayMode.Month;

							mode = DisplayMode;

							CalendarViewDisplayMode newMode = mode;
							switch (mode)
							{
								case CalendarViewDisplayMode.Month:
									if (key == VirtualKey.Up)
									{
										newMode = CalendarViewDisplayMode.Year;
									}

									break;
								case CalendarViewDisplayMode.Year:
									if (key == VirtualKey.Up)
									{
										newMode = CalendarViewDisplayMode.Decade;
									}
									else
									{
										newMode = CalendarViewDisplayMode.Month;
									}

									break;
								case CalendarViewDisplayMode.Decade:
									if (key == VirtualKey.Down)
									{
										newMode = CalendarViewDisplayMode.Year;
									}

									break;
								default:
									global::System.Diagnostics.Debug.Assert(false);
									break;
							}

							if (newMode != mode)
							{
								handled = true;
								// after display mode changed, we want the new item to be focused by keyboard.
								m_focusItemAfterDisplayModeChanged = true;
								m_focusStateAfterDisplayModeChanged = FocusState.Keyboard;
								DisplayMode = newMode;
							}
						}
						break;
					default:
						break;
				}
			}

			if (handled)
			{
				pArgs.Handled = true;
			}

			return;
		}

		private bool OnKeyboardNavigation(
			VirtualKey key,
			VirtualKey originalKey)
		{
			CalendarViewGeneratorHost spHost;

			GetActiveGeneratorHost(out spHost);
			var pHandled = false;

			var pPanel = spHost.Panel;
			if (pPanel is { })
			{
				int lastFocusedIndex = -1;

				lastFocusedIndex = spHost.CalculateOffsetFromMinDate(m_lastDisplayedDate);

				global::System.Diagnostics.Debug.Assert(lastFocusedIndex >= 0);
				int newFocusedIndex = lastFocusedIndex;
				switch (key)
				{
					// Home/End goes to the first/last date in current scope.
					case VirtualKey.Home:
					case VirtualKey.End:
						{
							var newFocusedDate = key == VirtualKey.Home ? spHost.GetMinDateOfCurrentScope() : spHost.GetMaxDateOfCurrentScope();
							newFocusedIndex = spHost.CalculateOffsetFromMinDate(newFocusedDate);
							global::System.Diagnostics.Debug.Assert(newFocusedIndex >= 0);
						}
						break;
					// PageUp/PageDown goes to the previous/next scope
					case VirtualKey.PageUp:
					case VirtualKey.PageDown:
						{
							var newFocusedDate = m_lastDisplayedDate;
							try
							{
								spHost.AddScopes(newFocusedDate, key == VirtualKey.PageUp ? -1 : 1);
								// probably beyond {min, max}, let's make sure.
								CoerceDate(ref newFocusedDate);
								newFocusedIndex = spHost.CalculateOffsetFromMinDate(newFocusedDate);
								global::System.Diagnostics.Debug.Assert(newFocusedIndex >= 0);

							}
							catch
							{
								// beyond calendar's limit, fall back to min/max date, i.e. the first/last item.
								if (key == VirtualKey.PageUp)
								{
									newFocusedIndex = 0;
								}
								else
								{
									uint size = 0;
									size = spHost.Size();
									newFocusedIndex = (int)(size - 1);
								}
							}
						}
						break;
					// Arrow keys: use the panel's default behavior.
					case VirtualKey.Left:
					case VirtualKey.Up:
					case VirtualKey.Right:
					case VirtualKey.Down:
						{
							KeyNavigationAction action = KeyNavigationAction.Up;
							uint newFocusedIndexUint = 0;
							var newFocusedType = ElementType.ItemContainer;
							bool isValidKey = false;
							bool actionValidForSourceIndex = false;

							TranslateKeyToKeyNavigationAction(key, out action, out isValidKey);
							global::System.Diagnostics.Debug.Assert(isValidKey);
							pPanel.GetTargetIndexFromNavigationAction(
								lastFocusedIndex,
								ElementType.ItemContainer,
								action,
								false, // !XboxUtility.IsGamepadNavigationDirection(originalKey),  /* allowWrap */
								-1,  /* itemIndexHintForHeaderNavigation */
								out newFocusedIndexUint,
								out newFocusedType,
								out actionValidForSourceIndex);

							global::System.Diagnostics.Debug.Assert(newFocusedType == ElementType.ItemContainer);

							if (actionValidForSourceIndex)
							{
								newFocusedIndex = (int)(newFocusedIndexUint);
							}
						}
						break;
					default:
						global::System.Diagnostics.Debug.Assert(false);
						break;
				}

				// Check if we can focus on a new index or not.
				if (newFocusedIndex != lastFocusedIndex)
				{
					FocusItemByIndex(spHost, newFocusedIndex, FocusState.Keyboard, out pHandled);
				}
			}

			return pHandled;
		}



		// Given a key, returns a focus navigation action.
		private void TranslateKeyToKeyNavigationAction(
			VirtualKey key,
			out KeyNavigationAction pNavAction,
			out bool pIsValidKey)
		{
			FlowDirection flowDirection = FlowDirection.LeftToRight;

			pIsValidKey = false;
			pNavAction = KeyNavigationAction.Up;

			if (m_tpViewsGrid is { })
			{
				flowDirection = (m_tpViewsGrid as Grid).FlowDirection;
			}
			else
			{
				flowDirection = FlowDirection;
			}

			KeyboardNavigation.TranslateKeyToKeyNavigationAction(flowDirection, key, out pNavAction, out pIsValidKey);

		}

		// When an item got focus, we save the date of this item (to m_lastDisplayedDate).
		// When we switching the view mode, we always focus this date.
		internal void OnItemFocused(CalendarViewBaseItem pItem)
		{
			DateTime date;
			CalendarViewDisplayMode mode = CalendarViewDisplayMode.Month;

			date = pItem.DateBase;

			mode = DisplayMode;

			CopyDate(
				mode,
				date,
				ref m_lastDisplayedDate);

		}


		private void FocusItemByIndex(
			CalendarViewGeneratorHost pHost,
			int index,
			FocusState focusState,
			out bool pFocused)
		{
			DateTime date;

			pFocused = false;

			global::System.Diagnostics.Debug.Assert(index >= 0);

			date = pHost.GetDateAt((uint)index);
			FocusItem(pHost, date, index, focusState, out pFocused);

			return;
		}

		private void FocusItemByDate(
			CalendarViewGeneratorHost pHost,
			DateTime date,
			FocusState focusState,
			out bool pFocused)
		{
			int index = -1;

			pFocused = false;

			index = pHost.CalculateOffsetFromMinDate(date);
			FocusItem(pHost, date, index, focusState, out pFocused);

			return;
		}

		private void FocusItem(
			CalendarViewGeneratorHost pHost,
			DateTime date,
			int index,
			FocusState focusState,
			out bool pFocused)
		{
			DependencyObject spItemAsI;
			CalendarViewBaseItem spItem;

			pFocused = false;

			SetDisplayDateInternal(date); // scroll item into view so we can move focus to it.

			spItemAsI = pHost.Panel.ContainerFromIndex(index);

			// Uno workaround
			if (spItemAsI is null)
			{
				// The scrolling might occurs async, especially on wasm.
				// For safety we prefer to ignore this instead of crashing which might cause exceptions worst than loosing the focus.
				return;
			}

			global::System.Diagnostics.Debug.Assert(spItemAsI is { });
			spItem = (CalendarViewBaseItem)spItemAsI;

			pFocused = spItem.FocusSelfOrChild(focusState);

			return;
		}

		// UIElement override for getting next tab stop on path from focus candidate element to root.
		internal override TabStopProcessingResult ProcessCandidateTabStopOverride(
			DependencyObject focusedElement,
			DependencyObject candidateTabStopElement,
			DependencyObject overriddenCandidateTabStopElement,
			bool isBackward)
		{
			// There is no ICalendarViewBaseItem interface so we can't use ctl.is to check an element is CalendarViewBaseItem or not
			// because in ctl.is, it will call ReleaseInterface and CalendarViewBaseItem has multiple interfaces.
			// ComPtr will do this in a smarter way - it always casts to IUnknown before release/addso there is no ambiguity.
			CalendarViewBaseItem spCandidateAsCalendarViewBaseItem = candidateTabStopElement as CalendarViewBaseItem;

			DependencyObject newTabStop = null;
			var isCandidateTabStopOverridden = false;

			// Check if the candidate is a calendaritem and the currently focused is not a calendaritem.
			// Which means we Tab (or shift+Tab) into the scrollviewer and we are going to put focus on the candidate.
			// However the candidate is the first (or last if we shift+Tab) realized item in the Panel, we should use the last
			// focused item to override the candidate (and ignore isBackward, i.e. Shift).

			if (spCandidateAsCalendarViewBaseItem is { })
			{
				CalendarViewBaseItem spFocusedAsCalendarViewBaseItem = focusedElement as CalendarViewBaseItem;
				if (spFocusedAsCalendarViewBaseItem is null)
				{
					CalendarViewGeneratorHost spHost;
					GetActiveGeneratorHost(out spHost);

					var pScrollViewer = spHost.ScrollViewer;
					if (pScrollViewer is { })
					{
						KeyboardNavigationMode mode = KeyboardNavigationMode.Local;

						mode = pScrollViewer.TabNavigation;
						// The tab navigation on this scrollviewer must be Once (it's default value in the template).
						// For other modes (Local or Cycle) we don't want to override.
						if (mode == KeyboardNavigationMode.Once)
						{
							// Are we tabbing from/to another view?
							// if developer makes other view focusable by re-template and the candidate is not
							// in the active view, we'll not override the candidate.
							var isAncestor = pScrollViewer.IsAncestorOf(candidateTabStopElement);

							if (isAncestor)
							{
								var pPanel = spHost.Panel;
								if (pPanel is { })
								{
									DependencyObject spContainer;
									//CalendarViewGeneratorHost spHost;
									GetActiveGeneratorHost(out spHost);

									var index = spHost.CalculateOffsetFromMinDate(m_lastDisplayedDate);

									// This container might not have focus so it could be recycled, bring
									// it into view so it can take focus.
									pPanel.ScrollItemIntoView(
										index,
										ScrollIntoViewAlignment.Default,
										0.0,  /* offset */
										true /* forceSynchronous */);

									spContainer = pPanel.ContainerFromIndex(index);

									global::System.Diagnostics.Debug.Assert(spContainer is { });

									newTabStop = spContainer;
									isCandidateTabStopOverridden = true;
								}
							}
						}
					}
				}
			}

			return new(isCandidateTabStopOverridden, newTabStop);
		}
	}
}
