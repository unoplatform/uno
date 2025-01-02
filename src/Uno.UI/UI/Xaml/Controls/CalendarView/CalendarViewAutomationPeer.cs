// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Windows.Globalization.DateTimeFormatting;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls.Primitives;
using DirectUI;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarView
	{
		internal class CalendarViewAutomationPeer : CalendarViewBaseItem.CalendarViewBaseItemAutomationPeer
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

#if false
			// Properties.
			private bool CanSelectMultipleImpl
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

			private bool IsSelectionRequiredImpl
			{
				get
				{
					bool pValue;
					pValue = false;
					return pValue;
				}
			}

			private bool IsReadOnlyImpl
			{
				get
				{
					bool pValue;
					pValue = true;
					return pValue;
				}
			}

			// This will be date string if single date is selected otherwise the name of header of view
			private string ValueImpl
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

			private RowOrColumnMajor RowOrColumnMajorImpl
			{
				get
				{
					RowOrColumnMajor pValue;
					pValue = RowOrColumnMajor.RowMajor;
					return pValue;
				}
			}

			private int ColumnCountImpl
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

			private int RowCountImpl
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

			// This will be visible rows in the view, real number of rows will not provide any value
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
								//uint allocSize = sizeof(IIRawElementProviderSimple) * realizedCount;
								//ppReturnValue = (IIRawElementProviderSimple*)(CoTaskMemAlloc(allocSize));
								//IFCOOMFAILFAST(ppReturnValue);
								//ZeroMemory(ppReturnValue, allocSize);
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

			private void SetValueImpl(string value)
			{
				throw new NotImplementedException();
			}

			// This will returns the header text labels on the top only in the case of monthView
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

						//uint allocSize = sizeof(IIRawElementProviderSimple) * nCount;
						//ppReturnValue = (IIRawElementProviderSimple*)(CoTaskMemAlloc(allocSize));
						//IFCOOMFAILFAST(ppReturnValue);
						//ZeroMemory(ppReturnValue, allocSize);
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

			private void GetRowHeadersImpl(out uint pReturnValueCount, out IRawElementProviderSimple[] ppReturnValue)
			{
				pReturnValueCount = 0;
				ppReturnValue = default;
				return;
			}


			private void GetItemImpl(int row, int column, out IRawElementProviderSimple ppReturnValue)
			{
				ppReturnValue = null;

				UIElement spOwner;
				spOwner = Owner;

				CalendarViewGeneratorHost spHost;
				(spOwner as CalendarView).GetActiveGeneratorHost(out spHost);

				DependencyObject spItemAsI;
				CalendarViewBaseItem spItem;
				AutomationPeer spItemPeerAsAP;
				IRawElementProviderSimple spProvider;

				int colCount = 0;
				colCount = ColumnCountImpl;

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
						spItemAsI = pCalendarPanel.ContainerFromIndex(itemIndex);
						// This can be a virtualized item or item does not exist, check for null
						if (spItemAsI is { })
						{
							spItem = (CalendarViewBaseItem)spItemAsI;

							spItemPeerAsAP = spItem.GetAutomationPeer();
							spProvider = ProviderFromPeer(spItemPeerAsAP);
							ppReturnValue = spProvider;
						}
					}
				}

				return;
			}
#endif

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
