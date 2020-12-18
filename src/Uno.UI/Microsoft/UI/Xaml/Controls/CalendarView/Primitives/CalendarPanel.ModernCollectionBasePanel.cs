using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Uno;

namespace Windows.UI.Xaml.Controls.Primitives
{
	// This file is aimed to implement methods that should be implemented by the ModernCollectionBasePanel which is not present in Uno

	partial class CalendarPanel : ILayoutDataInfoProvider
	{
		internal event VisibleIndicesUpdatedEventCallback VisibleIndicesUpdated;

		private CalendarLayoutStrategy _layoutStrategy;
		private CalendarViewGeneratorHost _host;

		internal void RegisterItemsHost(CalendarViewGeneratorHost pHost)
		{
			_host = pHost;
		}

		internal void DisconnectItemsHost()
		{
			_host = null;
		}

		internal int FirstVisibleIndexBase { get; }
		internal int LastVisibleIndexBase { get; }
		internal int FirstCacheIndexBase { get; }
		internal int LastCacheIndexBase { get; }

		[NotImplemented]
		internal PanelScrollingDirection PanningDirectionBase { get; } = PanelScrollingDirection.None;

		internal DependencyObject ContainerFromIndex(int firstVisibleIndex)
		{

			
			//_layoutStrategy.
			//_host.GetContainerForItem()

			return default;
		}

		[NotImplemented]
		internal void ScrollItemintoView(int index, ScrollIntoViewAlignment alignment, double offset, bool forceSynchronous)
		{
		}

		[NotImplemented]
		internal void ScrollItemIntoView(int index, ScrollIntoViewAlignment alignment, double offset, bool forceSynchronous)
		{
		}

		private Size GetViewportSize()
		{
			return new Size(300,300);
		}

		internal Size GetDesiredViewportSize()
		{
			return _layoutStrategy.GetDesiredViewportSize();
		}

		[NotImplemented]
		internal void GetTargetIndexFromNavigationAction(
			int focusedIndex,
			ElementType elementType,
			KeyNavigationAction action,
			object o,
			int i,
			out uint newFocusedIndexUint,
			out ElementType newFocusedType,
			out bool actionValidForSourceIndex)
		{
			newFocusedIndexUint = (uint)focusedIndex;
			newFocusedType = elementType;
			actionValidForSourceIndex = true;
		}

		internal IItemContainerMapping GetItemContainerMapping()
		{
			throw new NotImplementedException();
		}

		internal ILayoutStrategy LayoutStrategy => _layoutStrategy;

		internal double CacheLengthBase { get; set; }

		internal ContainerManager ContainerManager { get; } = new ContainerManager();

		private void SetLayoutStrategyBase(CalendarLayoutStrategy spLayoutStrategy)
		{
			_layoutStrategy = spLayoutStrategy;
			spLayoutStrategy.SetLayoutDataInfoProvider(this);
		}

		private void CacheFirstVisibleElementBeforeOrientationChange()
		{
		}

		private void ProcessOrientationChange()
		{
		}

		/// <inheritdoc />
		int ILayoutDataInfoProvider.GetTotalItemCount()
			=> ContainerManager.TotalItemsCount;

		/// <inheritdoc />
		int ILayoutDataInfoProvider.GetTotalGroupCount()
			=> ContainerManager.TotalGroupCount;
	}

	internal class ContainerManager
	{
		public int StartOfContainerVisualSection()
		{
			return 0;
		}

		public int TotalItemsCount { get; set; }

		public int TotalGroupCount { get; set; }
	}
}
