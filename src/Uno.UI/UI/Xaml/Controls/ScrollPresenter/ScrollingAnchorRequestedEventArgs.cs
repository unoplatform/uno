// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

public sealed partial class ScrollingAnchorRequestedEventArgs
{
	internal ScrollingAnchorRequestedEventArgs(ScrollPresenter scrollPresenter)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_PTR, METH_NAME, this, scrollPresenter);

		m_scrollPresenter = scrollPresenter;
	}

	#region IScrollingAnchorRequestedEventArgs

	public IList<UIElement> AnchorCandidates => m_anchorCandidates;

	public UIElement AnchorElement
	{
		get => GetAnchorElement();
		set
		{
			// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_PTR, METH_NAME, this, value);

			UIElement anchorElement = value;
			ScrollPresenter scrollPresenter = m_scrollPresenter;

			if (anchorElement is null || scrollPresenter.IsElementValidAnchor(anchorElement))
			{
				m_anchorElement = anchorElement;
			}
			else
			{
				throw new ArgumentException();
			}
		}
	}

	#endregion

	internal IList<UIElement> GetAnchorCandidates() => m_anchorCandidates;

	internal void SetAnchorCandidates(List<UIElement> anchorCandidates)
	{
		List<UIElement> anchorCandidatesTmp = new();

		foreach (var anchorCandidate in anchorCandidates)
		{
			anchorCandidatesTmp.Add(anchorCandidate);
		}
		m_anchorCandidates = anchorCandidatesTmp;
	}

	internal UIElement GetAnchorElement()
	{
		return m_anchorElement;
	}

	internal void SetAnchorElement(UIElement anchorElement)
	{
		m_anchorElement = anchorElement;
	}
}
