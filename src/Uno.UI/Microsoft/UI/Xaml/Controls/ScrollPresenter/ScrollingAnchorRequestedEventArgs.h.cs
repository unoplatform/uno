// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls;

public sealed partial class ScrollingAnchorRequestedEventArgs
{
	// ~ScrollingAnchorRequestedEventArgs()
	// {
	//     SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	private IList<UIElement> m_anchorCandidates;
	private UIElement m_anchorElement;
	private ScrollPresenter m_scrollPresenter;
};
