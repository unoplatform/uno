// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Windows.UI.Xaml.Controls;

internal partial class ViewChange : ViewChangeBase
{
	public ViewChange(
		ScrollPresenterViewKind viewKind,
		object options)
	{
		m_viewKind = viewKind;
		m_options = options;
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_PTR_STR, METH_NAME, this,
		// 	options,
		// 	TypeLogging::ScrollPresenterViewKindToString(viewKind).c_str());
	}

	// ~ViewChange()
	// {
	// 	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }
}
