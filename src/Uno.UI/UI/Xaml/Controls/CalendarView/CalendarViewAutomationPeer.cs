// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Globalization.DateTimeFormatting;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Microsoft.UI.Xaml.Controls
{
	partial class CalendarView
	{
		internal class CalendarViewAutomationPeer : CalendarViewBaseItem.CalendarViewBaseItemAutomationPeer,
			ITableProvider, IGridProvider, IValueProvider, ISelectionProvider
		{
			private const uint BulkChildrenLimit = 20;

			internal CalendarViewAutomationPeer(CalendarView owner) : base(owner)
			{
			}

			protected override object GetPatternCore(PatternInterface patternInterface)
			{
				object ppReturnValue = null;

				if (patternInterface == PatternInterface.Table ||
					patternInterface == PatternInterface.Grid ||
					patternInterface == PatternInterface.Value ||
					patternInterface == PatternInterface.Selection)
				{
					ppReturnValue = this;
				}
				else
				{
					ppReturnValue = base.GetPatternCore(patternInterface);
				}

				return ppReturnValue;
			}

			protected override string GetClassNameCore()
			{
				string pReturnValue;
				pReturnValue = "CalendarView";

				return pReturnValue;
			}

			protected override AutomationControlType GetAutomationControlTypeCore()
			{
				AutomationControlType pReturnValue;
				pReturnValue = AutomationControlType.Calendar;

				return pReturnValue;
			}

			protected override IList<AutomationPeer> GetChildrenCore()
			{
				IList<AutomationPeer> ppReturnValue;

				UIElement spOwner;
				spOwner = Owner;

				ppReturnValue = base.GetChildrenCore();
				if (ppReturnValue != null)
				{
					RemoveAPs(ppReturnValue);
				}

				return ppReturnValue;
			}

			// This function removes views that are not active.
			void RemoveAPs(IList<AutomationPeer> pAPCollection)
			{
				UIElement spOwner;
				spOwner = Owner;

				uint count = 0;
				count = (uint)pAPCollection.Count;
				for (int index = (int)count - 1; index >= 0; index--)
				{
					AutomationPeer spAutomationPeer;
					spAutomationPeer = pAPCollection[index];
					if (spAutomationPeer != null)
					{
						UIElement spCurrent;
						spCurrent = (spAutomationPeer as FrameworkElementAutomationPeer).Owner;

						while (spCurrent != null && spCurrent != spOwner)
						{
							double opacity = 1.0;
							opacity = spCurrent.Opacity;
							if (opacity == 0.0)
							{
								pAPCollection.RemoveAt(index);
								break;
							}

							DependencyObject spParent;
							spParent = (spCurrent as FrameworkElement).Parent;
							spCurrent = spParent as UIElement;
						}
					}
				}

				return;
			}

			// IValueProvider — exposes the selected date string, or the active view header
			// (month/year/decade label) when no single date is selected. Read-only because
			// callers select via SelectedDates, not via this pattern.

			public string Value
			{
				get
				{
					var calendar = (CalendarView)Owner;
					if (calendar.m_tpSelectedDates is { } selectedDates && selectedDates.Size == 1)
					{
						calendar.CreateDateTimeFormatter("day month.full year", out var formatter);
						return formatter.Format(selectedDates.GetAt(0));
					}

					calendar.GetActiveGeneratorHost(out var host);
					return host?.GetHeaderTextOfCurrentScope() ?? string.Empty;
				}
			}

			public bool IsReadOnly => true;

			public void SetValue(string value) => throw new InvalidOperationException();

			// ISelectionProvider — multi-select is reported only when CalendarView is in
			// Multiple mode; the returned providers correspond to realized CalendarViewDayItems.

			public bool CanSelectMultiple
				=> ((CalendarView)Owner).SelectionMode == CalendarViewSelectionMode.Multiple;

			public bool IsSelectionRequired => false;

			public IRawElementProviderSimple[] GetSelection()
			{
				var calendar = (CalendarView)Owner;
				if (calendar.DisplayMode != CalendarViewDisplayMode.Month)
				{
					return Array.Empty<IRawElementProviderSimple>();
				}

				if (calendar.m_tpSelectedDates is not { } selectedDates || selectedDates.Size == 0)
				{
					return Array.Empty<IRawElementProviderSimple>();
				}

				calendar.GetActiveGeneratorHost(out var host);
				var panel = host?.Panel;
				if (panel is null)
				{
					return Array.Empty<IRawElementProviderSimple>();
				}

				var realized = new List<IRawElementProviderSimple>((int)selectedDates.Size);
				for (uint i = 0; i < selectedDates.Size; i++)
				{
					var date = selectedDates.GetAt(i);
					var itemIndex = host.CalculateOffsetFromMinDate(date);
					if (panel.ContainerFromIndex(itemIndex) is CalendarViewBaseItem item
						&& item.GetAutomationPeer() is { } itemPeer
						&& ProviderFromPeer(itemPeer) is { } provider)
					{
						realized.Add(provider);
					}
				}

				return realized.ToArray();
			}

			// ITableProvider — week-day labels at the top serve as column headers; there
			// are no row headers in the Month view.

			public Microsoft.UI.Xaml.Automation.RowOrColumnMajor RowOrColumnMajor
				=> Microsoft.UI.Xaml.Automation.RowOrColumnMajor.RowMajor;

			public IRawElementProviderSimple[] GetColumnHeaders()
			{
				var calendar = (CalendarView)Owner;
				if (calendar.DisplayMode != CalendarViewDisplayMode.Month)
				{
					return Array.Empty<IRawElementProviderSimple>();
				}

				if (calendar.GetTemplateChild<Grid>("WeekDayNames") is not { } weekDayNames)
				{
					return Array.Empty<IRawElementProviderSimple>();
				}

				var headers = new List<IRawElementProviderSimple>(weekDayNames.Children.Count);
				foreach (var child in weekDayNames.Children)
				{
					if (child is FrameworkElement fe
						&& fe.GetAutomationPeer() is { } childPeer
						&& ProviderFromPeer(childPeer) is { } provider)
					{
						headers.Add(provider);
					}
				}

				return headers.ToArray();
			}

			public IRawElementProviderSimple[] GetRowHeaders()
				=> Array.Empty<IRawElementProviderSimple>();

			// IGridProvider — projects the active view's CalendarPanel as a grid.

			public int ColumnCount
			{
				get
				{
					var calendar = (CalendarView)Owner;
					calendar.GetActiveGeneratorHost(out var host);
					return host?.Panel?.Cols ?? 0;
				}
			}

			public int RowCount
			{
				get
				{
					var calendar = (CalendarView)Owner;
					calendar.GetActiveGeneratorHost(out var host);
					return host?.Panel?.Rows ?? 0;
				}
			}

			public IRawElementProviderSimple GetItem(int row, int column)
			{
				var calendar = (CalendarView)Owner;
				calendar.GetActiveGeneratorHost(out var host);
				var panel = host?.Panel;
				if (panel is null)
				{
					return null;
				}

				var colCount = panel.Cols;
				var firstVisibleIndex = panel.FirstVisibleIndex;
				var itemIndex = firstVisibleIndex + row * colCount + column;

				// firstVisibleIndex sits on the first row; subtract any leading offset so
				// (row=0, column=0) maps to the first cell as drawn (matches WinUI).
				if (firstVisibleIndex < colCount)
				{
					itemIndex -= panel.StartIndex;
				}

				if (itemIndex < 0)
				{
					return null;
				}

				if (panel.ContainerFromIndex(itemIndex) is CalendarViewBaseItem item
					&& item.GetAutomationPeer() is { } itemPeer)
				{
					return ProviderFromPeer(itemPeer);
				}

				return null;
			}

			internal void RaiseSelectionEvents(CalendarViewSelectedDatesChangedEventArgs pSelectionChangedEventArgs)
			{
				UIElement spOwner;
				spOwner = Owner;

				CalendarViewDisplayMode mode = CalendarViewDisplayMode.Month;
				mode = (spOwner as CalendarView).DisplayMode;

				// No header for year or decade view
				if (mode == CalendarViewDisplayMode.Month)
				{
					CalendarViewGeneratorHost spHost;
					(spOwner as CalendarView).GetActiveGeneratorHost(out spHost);

					var pPanel = spHost.Panel;
					if (pPanel is { })
					{
						DateTime date;
						DependencyObject spItemAsI;
						CalendarViewBaseItem spItem;
						AutomationPeer spItemPeerAsAP;
						int itemIndex = 0;
						uint selectedCount = 0;

						selectedCount = (spOwner as CalendarView).m_tpSelectedDates.Size;

						IReadOnlyList<DateTimeOffset> spAddedDates;
						IReadOnlyList<DateTimeOffset> spRemovedDates;
						uint addedDatesSize = 0;
						uint removedDatesSize = 0;

						spAddedDates = pSelectionChangedEventArgs.AddedDates;
						addedDatesSize = (uint)spAddedDates.Count;

						spRemovedDates = pSelectionChangedEventArgs.RemovedDates;
						removedDatesSize = (uint)spRemovedDates.Count;

						// One selection added and that is the only selection
						if (addedDatesSize == 1 && selectedCount == 1)
						{

							date = spAddedDates[0];
							itemIndex = spHost.CalculateOffsetFromMinDate(date);
							spItemAsI = pPanel.ContainerFromIndex(itemIndex);
							if (spItemAsI is { })
							{
								spItem = (CalendarViewBaseItem)spItemAsI;
								spItemPeerAsAP = spItem.GetAutomationPeer();
								if (spItemPeerAsAP is { })
								{
									spItemPeerAsAP.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementSelected);
								}
							}

							if (removedDatesSize == 1)
							{
								date = spRemovedDates[0];
								itemIndex = spHost.CalculateOffsetFromMinDate(date);
								spItemAsI = pPanel.ContainerFromIndex(itemIndex);
								if (spItemAsI is { })
								{
									spItem = (CalendarViewBaseItem)spItemAsI;
									spItemPeerAsAP = spItem.GetAutomationPeer();
									if (spItemPeerAsAP is { })
									{
										spItemPeerAsAP.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
									}
								}
							}
						}
						else
						{
							if (addedDatesSize + removedDatesSize > BulkChildrenLimit)
							{
								RaiseAutomationEvent(AutomationEvents.SelectionPatternOnInvalidated);
							}
							else
							{
								uint i = 0;
								for (i = 0; i < addedDatesSize; i++)
								{
									date = spAddedDates[(int)i];
									itemIndex = spHost.CalculateOffsetFromMinDate(date);
									spItemAsI = pPanel.ContainerFromIndex(itemIndex);
									if (spItemAsI is { })
									{
										spItem = (CalendarViewBaseItem)spItemAsI;
										spItemPeerAsAP = spItem.GetAutomationPeer();
										if (spItemPeerAsAP is { })
										{
											spItemPeerAsAP.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementAddedToSelection);
										}
									}
								}

								for (i = 0; i < removedDatesSize; i++)
								{
									date = spRemovedDates[(int)i];
									itemIndex = spHost.CalculateOffsetFromMinDate(date);
									spItemAsI = pPanel.ContainerFromIndex(itemIndex);
									if (spItemAsI is { })
									{
										spItem = (CalendarViewBaseItem)spItemAsI;
										spItemPeerAsAP = spItem.GetAutomationPeer();
										if (spItemPeerAsAP is { })
										{
											spItemPeerAsAP.RaiseAutomationEvent(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection);
										}
									}
								}
							}
						}
					}
				}

				return;
			}
		}
	}
}
