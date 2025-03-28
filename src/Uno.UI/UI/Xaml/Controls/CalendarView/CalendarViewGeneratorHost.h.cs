using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Globalization;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using DateTime = Windows.Foundation.WindowsFoundationDateTime;

namespace Windows.UI.Xaml.Controls
{
	partial class CalendarViewGeneratorHost : IDirectManipulationStateChangeHandler
	{
		internal int[] GetLastVisibleIndicesPairRef() { return m_lastVisibleIndicesPair; }

		internal DateTime GetMinDateOfCurrentScope() { return m_minDateOfCurrentScope; }
		internal DateTime GetMaxDateOfCurrentScope() { return m_maxDateOfCurrentScope; }
		internal string GetHeaderTextOfCurrentScope() { return m_pHeaderText; }

		internal virtual void SetupContainerContentChangingAfterPrepare(
			DependencyObject pContainer,
			object pItem,
			int itemIndex,
			Size measureSize)
		{ }

		internal virtual void RaiseContainerContentChangingOnRecycle(
			UIElement pContainer,
			object pItem)
		{ }
	}
}
