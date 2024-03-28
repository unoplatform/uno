// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives
{
	partial class CalendarPanel
	{
		// protected:

		public CalendarPanel()
		{
			m_type = CalendarPanelType.Invalid;
			m_isBiggestItemSizeDetermined = false;
			m_biggestItemSize = default;
			m_suggestedRows = -1;
			m_suggestedCols = -1;

			// UNO Only
			Initialize();
		}

		//private void Initialize() override;

		//      IFACEMETHOD(MeasureOverride)(
		//          Size pAvailableSize,
		//          outSize pDesired) override;

		//      IFACEMETHOD(ArrangeOverride)(
		//          Size arrangeSize,
		//          outSize pReturnValue) override;

		//      // Handle the custom property changed event and call the
		//      // OnPropertyChanged methods. 
		//      private void OnPropertyChanged2(  PropertyChangedParams& args) override;

		// Only ItemsStackPanel and CalendarPanel support the maintain viewport behavior.
		//protected /* override */ ItemsUpdatingScrollMode ItemsUpdatingScrollMode => (ItemsUpdatingScrollMode)(ItemsUpdatingScrollMode.KeepItemsInView);

		// a wrapgrid is able to portal (AddDeleteTransition)
		//protected /* override */ bool IsPortallingSupported => true;

		//// Virtual helper method to get the ItemsPerPage that can be overridden by derived classes.
		//protected /* override */ void ItemsPerPageImpl(Rect window, out double pItemsPerPage);

		////
		//// Special elements overrides
		////
		//protected /* override */ void NeedsSpecialItem(out bool pResult);
		//protected /* override */ void GetSpecialItemIndex(out int pResult);

		//public:
		//public void GetDesiredViewportSize(outSize pSize);
		//public void SetItemMinimumSize(Size size);

		//private void SetSnapPointFilterFunction( std.function<HRESULT( int itemIndex, out bool pHasSnapPoint)> func);

		//private void SetOwner( CalendarViewGeneratorHost pOwner);

		//private void SetNeedsToDetermineBiggestItemSize();

		internal int FirstCacheIndexImpl => FirstCacheIndexBase;
		internal int FirstVisibleIndexImpl => FirstVisibleIndexBase;
		internal int LastVisibleIndexImpl => LastVisibleIndexBase;
		internal int LastCacheIndexImpl => LastCacheIndexBase;
		internal PanelScrollingDirection ScrollingDirectionImpl => PanningDirectionBase;

		//private void SetPanelType( CalendarPanelType type);

		//CalendarPanelType GetPanelType()
		//{
		//    return m_type;
		//}

		//private void SetSuggestedDimension( int cols,  int rows);
		//public:
		//// implementation of IOrientedPanel
		//IFACEMETHOD(get_LogicalOrientation)(out Orientation pValue) override;
		//IFACEMETHOD(get_PhysicalOrientation)(out Orientation pValue) override;

		//private:
		//private void OnRowsOrColsChanged( Orientation orientation);

		//private void GetOwner(out result_maybenull_ CalendarViewGeneratorHost* ppOwner);

		//private void DetermineTheBiggestItemSize(
		//     CalendarViewGeneratorHost pOwner,
		//    Size availableSize, 
		//    outSize pSize);

		//private void SetPanelDimension( int col,  int row);

		//private:
		WeakReference<CalendarViewGeneratorHost> m_wrGeneartorHostOwner;

		// primaryPanel will determine the whole CalendarView's size, e.g. MonthPanel
		// secondaryPanels will not, e.g. YearPanel and DecadePanel.

		// primaryPanel always reports it's real desired size to it's parent (SCP)
		// secondaryPanels always reports (0,0) as it's desired size to it's parent (SCP)

		private CalendarPanelType m_type;

		private bool m_isBiggestItemSizeDetermined;

		private Size m_biggestItemSize;

		private int m_suggestedRows;
		private int m_suggestedCols;
	}
}
