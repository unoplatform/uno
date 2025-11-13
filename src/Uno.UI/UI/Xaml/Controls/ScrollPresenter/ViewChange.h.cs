// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Microsoft.UI.Xaml.Controls;

internal enum ScrollPresenterViewKind
{
	Absolute,
	RelativeToCurrentView,
#if ScrollPresenterViewKind_RelativeToEndOfInertiaView // UNO TODO
    RelativeToEndOfInertiaView,
#endif
}

internal partial class ViewChange : ViewChangeBase
{
	public ScrollPresenterViewKind ViewKind()
	{
		return m_viewKind;
	}

	public object Options()
	{
		return m_options;
	}

	private ScrollPresenterViewKind m_viewKind = ScrollPresenterViewKind.Absolute;
	// ScrollingScrollOptions or ScrollingZoomOptions instance associated with this view change.
	private object m_options;
};

