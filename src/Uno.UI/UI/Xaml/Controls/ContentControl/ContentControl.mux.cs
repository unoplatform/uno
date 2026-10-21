using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

public partial class ContentControl
{
	internal override string GetPlainText()
	{
		var content = Content;

		if (content is not null)
		{
			return FrameworkElement.GetStringFromObject(content);
		}

		return null;
	}

	// MUX Reference ContentControl.cpp, lines 572-596, tag winui3/release/2.5.1
	private ListViewBaseItemPresenter m_pListViewBaseItemChrome;

	// Sets the Grid/ListViewItem-specific chrome. If we add CSelectorItem, move this and the associated field
	// there. This method takes and releases refs as required. Passing in NULL clears the chrome.
	internal void SetGridViewItemChrome(ListViewBaseItemPresenter pChrome)
	{
		if (m_pListViewBaseItemChrome is not null)
		{
			m_pListViewBaseItemChrome.SetChromedListViewBaseItem(null);
			m_pListViewBaseItemChrome = null;
		}

		if (pChrome is not null)
		{
			m_pListViewBaseItemChrome = pChrome;
		}
	}

	// Gets the Grid/ListViewItem-specific chrome.
	internal ListViewBaseItemPresenter GetGridViewItemChromeNoRef() => m_pListViewBaseItemChrome;
}
