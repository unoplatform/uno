// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Markup;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class Phaser
	{
		private readonly ItemsRepeater m_owner;
		private List<ElementInfo> m_pendingElements = new List<ElementInfo>();
		private bool m_registeredForCallback;

		public Phaser(ItemsRepeater owner)
		{
			// ItemsRepeater is not fully constructed yet. Don't interact with it.

			m_owner = owner;
		}

		public void PhaseElement(UIElement element, VirtualizationInfo virtInfo)
		{
			var dataTemplateComponent = virtInfo.DataTemplateComponent;
			var nextPhase = virtInfo.Phase;
			bool shouldPhase = false;

			if (nextPhase > 0)
			{
				if (dataTemplateComponent != null)
				{
					// Perf optimization: RecyclingElementFactory sets up the virtualization info so we don't need to update it here.
					shouldPhase = true;
				}
				else
				{
					throw new InvalidOperationException("Phase was set on virtualization info, but dataTemplateComponent was not.");
				}
			}
			else if (nextPhase == VirtualizationInfo.PhaseNotSpecified)
			{
				// If virtInfo.Phase is not specified, virtInfo.DataTemplateComponent cannot be valid.
				global::System.Diagnostics.Debug.Assert(dataTemplateComponent == null);
				// ItemsRepeater might be using a custom view generator in which case, virtInfo would not be bootstrapped.
				// In this case, fallback to querying for the data template component and setup virtualization info now.
				dataTemplateComponent = XamlBindingHelper.GetDataTemplateComponent(element);
				if (dataTemplateComponent != null)
				{
					// Clear out old data. 
					dataTemplateComponent.Recycle();

					nextPhase = VirtualizationInfo.PhaseReachedEnd;
					var index = virtInfo.Index;
					var data = m_owner.ItemsSourceView.GetAt(index);
					// Run Phase 0
					dataTemplateComponent.ProcessBindings(data, index, 0 /* currentPhase */, out nextPhase);

					// Update phase on virtInfo. Set data and templateComponent only if x:Phase was used.
					virtInfo.UpdatePhasingInfo(nextPhase, nextPhase > 0 ? data : null, nextPhase > 0 ? dataTemplateComponent : null);
					shouldPhase = nextPhase > 0;
				}
			}

			if (shouldPhase)
			{
				// Insert at the top since we remove from bottom during DoPhasedWorkCallback. This keeps the ordering of items
				// the same as the order in which items are realized.
				m_pendingElements.Insert(0, new ElementInfo(element, virtInfo));
				RegisterForCallback();
			}
		}

		public void StopPhasing(UIElement element, VirtualizationInfo virtInfo)
		{
			// We need to remove the element from the pending elements list. We cannot just change the phase to -1
			// since it will get updated when the element gets recycled.
			if (virtInfo.DataTemplateComponent != null)
			{
				var it = m_pendingElements.FindIndex(info => info.Element == element);
				if (it != -1)
				{
					m_pendingElements.RemoveAt(it);
				}
			}

			// Clean Phasing information for this item.
			virtInfo.UpdatePhasingInfo(VirtualizationInfo.PhaseNotSpecified, null /* data */, null /* dataTemplateComponent */);
		}

		void DoPhasedWorkCallback()
		{
			MarkCallbackRecieved();

			if (m_pendingElements.Count > 0 && !BuildTreeScheduler.ShouldYield())
			{
				var visibleWindow = m_owner.VisibleWindow;
				SortElements(visibleWindow);
				int currentIndex = m_pendingElements.Count - 1;
				do
				{
					var info = m_pendingElements[currentIndex];
					var element = info.Element;
					var virtInfo = info.VirtInfo;
					var dataIndex = virtInfo.Index;

					int currentPhase = virtInfo.Phase;
					if (currentPhase > 0)
					{
						int nextPhase = VirtualizationInfo.PhaseReachedEnd;
						virtInfo.DataTemplateComponent.ProcessBindings(virtInfo.Data, -1 /* item index unused */, currentPhase, out nextPhase);
						ValidatePhaseOrdering(currentPhase, nextPhase);

						var previousAvailableSize = LayoutInformation.GetAvailableSize(element);
						element.Measure(previousAvailableSize);

						if (nextPhase > 0)
						{
							virtInfo.Phase = nextPhase;
							// If we are the first item or 
							// If the current items phase is higher than the next items phase, then move to the next item.
							if (currentIndex == 0 ||
								virtInfo.Phase > m_pendingElements[currentIndex - 1].VirtInfo.Phase)
							{
								currentIndex--;
							}
						}
						else
						{
							m_pendingElements.RemoveAt(currentIndex);
							currentIndex--;
						}
					}
					else
					{
						throw new InvalidOperationException("Cleared element found in pending list which is not expected");
					}

					var pendingCount = (int)(m_pendingElements.Count);
					if (currentIndex == -1)
					{
						// Reached the top, start from the bottom again
						currentIndex = pendingCount - 1;
					}
					else if (currentIndex > -1 && currentIndex < pendingCount - 1)
					{
						// If the next element is oustide the visible window and there are elements in the visible window
						// go back to the visible window.
						bool nextItemIsVisible = SharedHelpers.DoRectsIntersect(visibleWindow, m_pendingElements[currentIndex].VirtInfo.ArrangeBounds);
						if (!nextItemIsVisible)
						{
							bool haveVisibleItems = SharedHelpers.DoRectsIntersect(visibleWindow, m_pendingElements[pendingCount - 1].VirtInfo.ArrangeBounds);
							if (haveVisibleItems)
							{
								currentIndex = pendingCount - 1;
							}
						}
					}
				} while (m_pendingElements.Count > 0 && !BuildTreeScheduler.ShouldYield());
			}

			if (m_pendingElements.Count > 0)
			{
				RegisterForCallback();
			}
		}

		void RegisterForCallback()
		{
			if (!m_registeredForCallback)
			{
				global::System.Diagnostics.Debug.Assert(m_pendingElements.Count != 0);
				m_registeredForCallback = true;
				BuildTreeScheduler.RegisterWork(
						m_pendingElements[m_pendingElements.Count - 1].VirtInfo.Phase,  // Use the phase of the last one in the sorted list
						DoPhasedWorkCallback);
			}
		}

		void MarkCallbackRecieved()
		{
			m_registeredForCallback = false;
		}

		/* static */
		void ValidatePhaseOrdering(int currentPhase, int nextPhase)
		{
			if (nextPhase > 0 && nextPhase <= currentPhase)
			{
				// nextPhase <= currentPhase is invalid
				throw new InvalidOperationException("Phases are required to be monotonically increasing.");
			}
		}

		void SortElements(Rect visibleWindow)
		{
			// Sort in descending order (inVisibleWindow, phase)
			m_pendingElements.Sort((lhs, rhs) =>
			{
				var lhsBounds = lhs.VirtInfo.ArrangeBounds;
				var lhsIntersects = SharedHelpers.DoRectsIntersect(lhsBounds, visibleWindow);
				var rhsBounds = rhs.VirtInfo.ArrangeBounds;
				var rhsIntersects = SharedHelpers.DoRectsIntersect(rhsBounds, visibleWindow);

				if ((lhsIntersects && rhsIntersects) ||
					(!lhsIntersects && !rhsIntersects))
				{
					// Both are in the visible window or both are not
					return lhs.VirtInfo.Phase - rhs.VirtInfo.Phase;
				}
				else if (lhsIntersects)
				{
					// Left is in visible window
					return -1;
				}
				else
				{
					return 1;
				}
			});
		}

		private struct ElementInfo
		{
			public ElementInfo(UIElement element, VirtualizationInfo virtInfo)
			{
				Element = element;
				VirtInfo = virtInfo;
			}

			public UIElement Element { get; }
			public VirtualizationInfo VirtInfo { get; }
		}
	}
}
