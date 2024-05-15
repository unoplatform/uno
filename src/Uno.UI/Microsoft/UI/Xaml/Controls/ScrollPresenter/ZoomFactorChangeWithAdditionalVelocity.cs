// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;

namespace Microsoft.UI.Xaml.Controls;

internal partial class ZoomFactorChangeWithAdditionalVelocity : ViewChangeBase
{
	public ZoomFactorChangeWithAdditionalVelocity(
		float zoomFactorVelocity,
		float anticipatedZoomFactorChange,
		Vector2? centerPoint,
		float? inertiaDecayRate)
	{
		m_zoomFactorVelocity = zoomFactorVelocity;
		m_anticipatedZoomFactorChange = anticipatedZoomFactorChange;
		m_centerPoint = centerPoint;
		m_inertiaDecayRate = inertiaDecayRate;
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_FLT_FLT, METH_NAME, this,
		// 	zoomFactorVelocity, anticipatedZoomFactorChange);
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_STR, METH_NAME, this,
		// 	TypeLogging::NullableFloat2ToString(centerPoint).c_str(), TypeLogging::NullableFloatToString(inertiaDecayRate).c_str());
	}

	// ~ZoomFactorChangeWithAdditionalVelocity()
	// {
	// 	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	public void ZoomFactorVelocity(float zoomFactorVelocity)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_FLT, METH_NAME, this, zoomFactorVelocity);

		m_zoomFactorVelocity = zoomFactorVelocity;
	}

	public void AnticipatedZoomFactorChange(float anticipatedZoomFactorChange)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_FLT, METH_NAME, this, anticipatedZoomFactorChange);

		m_anticipatedZoomFactorChange = anticipatedZoomFactorChange;
	}
}
