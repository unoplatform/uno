// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ScrollControllerAddScrollVelocityRequestedEventArgs.cpp, ScrollControllerAddScrollVelocityRequestedEventArgs.h, tag winui3/release/1.4.2

namespace Windows.UI.Xaml.Controls.Primitives;

public sealed partial class ScrollControllerAddScrollVelocityRequestedEventArgs
{
	//~ScrollControllerPanRequestedEventArgs()
	//{
	//	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	//}

	public ScrollControllerAddScrollVelocityRequestedEventArgs(
		float offsetVelocity,
		float? inertiaDecayRate)
	{
		//SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_FLT, METH_NAME, this,
		//	TypeLogging::NullableFloatToString(inertiaDecayRate).c_str(), offsetVelocity);

		OffsetVelocity = offsetVelocity;
		InertiaDecayRate = inertiaDecayRate;
	}

	public float OffsetVelocity { get; }

	public float? InertiaDecayRate { get; }

	public int CorrelationId { get; set; } = -1;
}
