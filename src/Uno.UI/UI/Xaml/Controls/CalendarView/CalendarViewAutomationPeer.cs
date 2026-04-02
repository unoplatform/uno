// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Globalization.DateTimeFormatting;
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
			ISelectionProvider, IValueProvider, ITableProvider, IGridProvider
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

			// ISelectionProvider
			public bool CanSelectMultiple
			{
				get
				{
					bool pValue;
					pValue = false;

					UIElement spOwner;
					spOwner = Owner;

					CalendarViewSelectionMode selectionMode = CalendarViewSelectionMode.None;
					selectionMode = (spOwner as CalendarView).SelectionMode;

					if (selectionMode == CalendarViewSelectionMode.Multiple)
					{
						pValue = true;
					}

					return pValue;
				}
			}

			public bool IsSelectionRequired
			{
				get
				{
					return false;
				}
			}

			public IRawElementProviderSimple[] GetSelection()
			{
				GetSelectionImpl(out _, out var result);
				return result ?? Array.Empty<IRawElementProviderSimple>();
			}

			// IValueProvider
			public bool IsReadOnly
			{
				get
				{
					return true;
				}
			}

			// This will be date string if single date is selected otherwise the name of header of view
			public string Value
			{
				get
				{
					string pValue = default;
					UIElement spOwner;
					spOwner = Owner;

					uint count = 0;
					count = (spOwner as CalendarView).m_tpSelectedDates.Size;
					if (count == 1)
					{
						string @string;
						DateTimeFormatter spFormatter;
						(spOwner as CalendarView).CreateDateTimeFormatter("day month.full year", out spFormatter);

						DateTime date;
						date = (spOwner as CalendarView).m_tpSelectedDates.GetAt(0);

						@string = spFormatter.Format(date);
						pValue = @string;
					}
					else
					{
						CalendarViewGeneratorHost spHost;
						(spOwner as CalendarView).GetActiveGeneratorHost(out spHost);
						pValue = spHost.GetHeaderTextOfCurrentScope();
					}

					return pValue;
				}
			}

			public void SetValue(string value)
			{
				throw new InvalidOperationException("CalendarView value is read-only.");
			}

			// ITableProvider
			public Automation.RowOrColumnMajor RowOrColumnMajor
			{
				get
				{
					return Automation.RowOrColumnMajor.RowMajor;
				}
			}

			public IRawElementProviderSimple[] GetColumnHeaders()
			{
				GetColumnHeadersImpl(out _, out var result);
				return result ?? Array.Empty<IRawElementProviderSimple>();
			}

			public IRawElementProviderSimple[] GetRowHeaders()
			{
				return Array.Empty<IRawElementProviderSimple>();
			}

			// IGridProvider
			public int ColumnCount
			{
				get
				{
					int pValue;
					pValue = 0;

					UIElement spOwner;
					spOwner = Owner;

					CalendarViewGeneratorHost spHost;
					(spOwner as CalendarView).GetActiveGeneratorHost(out spHost);
					CalendarPanel pCalendarPanel = spHost.Panel;
					if (pCalendarPanel is { })
					{
						pValue = pCalendarPanel.Cols;
					}

					return pValue;
				}
			}

			public int RowCount
			{
				get
				{
					int pValue;
					pValue = 0;

					UIElement spOwner;
					spOwner = Owner;

					CalendarViewGeneratorHost spHost;
					(spOwner as CalendarView).GetActiveGeneratorHost(out spHost);
					CalendarPanel pCalendarPanel = spHost.Panel;
					if (pCalendarPanel is { })
					{
						pValue = pCalendarPanel.Rows;
					}

					return pValue;
				}
			}

			public IRawElementProviderSimple GetItem(int row, int column)
			{
				GetItemImpl(row, column, out var result);
				return result;
			}

			// Internal implementation methods
			private void GetSelectionImpl(out uint pReturnValueCount, out IRawElementProviderSimple[] ppReturnValue)
			{
				UIElement spOwner;
				spOwner = Owner;
				pReturnValueCount = 0;
				ppReturnValue = default;

				CalendarViewDisplayMode mode = CalendarViewDisplayMode.Month;
				mode = (spOwner as CalendarView).DisplayMode;
				if (mode == CalendarViewDisplayMode.Month)
				{
					uint count = 0;
					uint realizedCount = 0;

					count = (spOwner as CalendarView).m_tpSelectedDates.Size;
					if (count > 0)
					{
						CalendarViewGeneratorHost spHost;
						(spOwner as CalendarView).GetActiveGeneratorHost(out spHost);

						IVector<AutomationPeer> spAPChildren;
						spAPChildren = new TrackerCollection<AutomationPeer>();

						var pPanel = spHost.Panel;
						if (pPanel is { })
						{
							for (uint i = 0; i < count; i++)
							{
								DateTime date;
								int itemIndex = 0;
								DependencyObject spItemAsI;
								CalendarViewBaseItem spItem;
								AutomationPeer spItemPeerAsAP;

								date = (spOwner as CalendarView).m_tpSelectedDates.GetAt(i);
								itemIndex = spHost.CalculateOffsetFromMinDate(date);

								spItemAsI = pPanel.ContainerFromIndex(itemIndex);
								if (spItemAsI is { })
								{
									spItem = (CalendarViewBaseItem)spItemAsI;
									spItemPeerAsAP = spItem.GetAutomationPeer();
									spAPChildren.Add(spItemPeerAsAP);
								}
							}

							realizedCount = (uint)spAPChildren.Count;
							if (realizedCount > 0)
							{
								ppReturnValue = new IRawElementProviderSimple[realizedCount];

								for (uint index = 0; index < realizedCount; index++)
								{
									AutomationPeer spItemPeerAsAP;
									IRawElementProviderSimple spProvider;
									spItemPeerAsAP = spAPChildren[(int)index];
									spProvider = ProviderFromPeer(spItemPeerAsAP);
									ppReturnValue[index] = spProvider;
								}
							}

							pReturnValueCount = realizedCount;
						}
					}
				}
			}

			private void GetColumnHeadersImpl(out uint pReturnValueCount, out IRawElementProviderSimple[] ppReturnValue)
			{
				pReturnValueCount = 0;
				ppReturnValue = default;
				UIElement spOwner;
				spOwner = Owner;

				CalendarViewDisplayMode mode = CalendarViewDisplayMode.Month;
				mode = (spOwner as CalendarView).DisplayMode;
				if (mode == CalendarViewDisplayMode.Month)
				{
					uint nCount = 0;
					Grid spWeekDayNames;
					spWeekDayNames = (spOwner as CalendarView).GetTemplateChild<Grid>("WeekDayNames");
					if (spWeekDayNames is { })
					{
						IList<UIElement> spChildren;
						spChildren = (spWeekDayNames as Grid).Children;
						nCount = (uint)spChildren.Count;

						ppReturnValue = new IRawElementProviderSimple[nCount];
						for (uint i = 0; i < nCount; i++)
						{
							AutomationPeer spItemPeerAsAP;
							IRawElementProviderSimple spProvider;
							UIElement spChild;
							spChild = spChildren[(int)i];

							spItemPeerAsAP = (spChild as FrameworkElement).GetAutomationPeer();
							spProvider = ProviderFromPeer(spItemPeerAsAP);
							(ppReturnValue)[i] = spProvider;
						}

						pReturnValueCount = nCount;
					}
				}
			}

			private void GetItemImpl(int row, int column, out IRawElementProviderSimple ppReturnValue)
			{
				ppReturnValue = null;

				UIElement spOwner;
				spOwner = Owner;

				CalendarViewGeneratorHost spHost;
				(spOwner as CalendarView).GetActiveGeneratorHost(out spHost);

				int colCount = 0;
				colCount = ColumnCount;

				int firstVisibleIndex = 0;
				CalendarPanel pCalendarPanel = spHost.Panel;
				if (pCalendarPanel is { })
				{
					firstVisibleIndex = pCalendarPanel.FirstVisibleIndex;

					int itemIndex = firstVisibleIndex + row * colCount + column;
					// firstVisibleIndex is on the first row, we need to count how many space before the firstVisibleIndex.
					if (firstVisibleIndex < colCount)
					{
						int startIndex = 0;
						startIndex = pCalendarPanel.StartIndex;
						itemIndex -= startIndex;
					}

					if (itemIndex >= 0)
					{
						DependencyObject spItemAsI;
						spItemAsI = pCalendarPanel.ContainerFromIndex(itemIndex);
						// This can be a virtualized item or item does not exist, check for null
						if (spItemAsI is { })
						{
							CalendarViewBaseItem spItem = (CalendarViewBaseItem)spItemAsI;
							AutomationPeer spItemPeerAsAP = spItem.GetAutomationPeer();
							IRawElementProviderSimple spProvider = ProviderFromPeer(spItemPeerAsAP);
							ppReturnValue = spProvider;
						}
					}
				}

				return;
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
