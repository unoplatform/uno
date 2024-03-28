// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.UI.Xaml.Automation.Peers;

namespace Windows.UI.Xaml.Controls
{
	internal class CalendarScrollViewerAutomationPeer : ScrollViewerAutomationPeer
	{
		public CalendarScrollViewerAutomationPeer(ScrollViewer owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "CalendarScrollViewer";
		}

		protected override IList<AutomationPeer> GetChildrenCore()
		{
			IList<AutomationPeer> ppReturnValue = default;

			UIElement spOwner;
			FrameworkElement spOwnerAsFrameworkElement;

			spOwner = Owner;
			spOwnerAsFrameworkElement = (FrameworkElement)spOwner;

			DependencyObject spTemplatedParent;
			spTemplatedParent = (spOwnerAsFrameworkElement as FrameworkElement).TemplatedParent;

			if (spTemplatedParent is { })
			{
				CalendarView spCalendarView = spTemplatedParent as CalendarView;

				if (spCalendarView is { })
				{
					CalendarViewGeneratorHost spGeneratorHost;
					(spCalendarView as CalendarView).GetActiveGeneratorHost(out spGeneratorHost);
					var pCalendarPanel = spGeneratorHost.Panel;

					if (pCalendarPanel is { })
					{
						int firstIndex = -1;
						int lastIndex = -1;

						firstIndex = pCalendarPanel.FirstVisibleIndex;
						lastIndex = pCalendarPanel.LastVisibleIndex;

						// This ScrollViewer automation peer ensures that for CalendarViews, accessible Items are restricted
						// to visible Items. To go to next unit view, user scenario is to utilize next and previous button.
						// Utilizing realized Items has a side effect due to bufferring, the first Item is a few months
						// back then current Item leading to an awkward state.
						if (firstIndex != -1 && lastIndex != -1)
						{
							List<AutomationPeer> spAPChildren;
							spAPChildren = new List<AutomationPeer>();

							for (int index = firstIndex; index <= lastIndex; ++index)
							{
								DependencyObject spChildAsIDO;
								CalendarViewBaseItem spChildAsItem;
								spChildAsIDO = pCalendarPanel.ContainerFromIndex(index);
								spChildAsItem = (CalendarViewBaseItem)spChildAsIDO;

								AutomationPeer spAutomationPeer;
								spAutomationPeer = (spChildAsItem as CalendarViewBaseItem).GetAutomationPeer();

								if (spAutomationPeer is { })
								{
									spAPChildren.Add(spAutomationPeer);
								}
							}

							ppReturnValue = spAPChildren;
						}
					}
				}
			}

			return ppReturnValue;
		}

	}
}
