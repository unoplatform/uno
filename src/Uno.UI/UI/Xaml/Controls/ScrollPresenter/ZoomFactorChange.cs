// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;

namespace Microsoft.UI.Xaml.Controls;

internal partial class ZoomFactorChange : ViewChange
{
	public ZoomFactorChange(
		float zoomFactor,
		Vector2? centerPoint,
		ScrollPresenterViewKind zoomFactorKind,
		object options) : base(zoomFactorKind, options)
	{
		m_zoomFactor = zoomFactor;
		m_centerPoint = centerPoint;
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_FLT, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(centerPoint).c_str(), zoomFactor);
	}

	// ~ZoomFactorChange()
	// {
	// 	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }
}
