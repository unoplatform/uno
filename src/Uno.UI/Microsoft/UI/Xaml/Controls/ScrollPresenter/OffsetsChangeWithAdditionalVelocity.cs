// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Numerics;

namespace Windows.UI.Xaml.Controls;

// Uno specific: inheriting from OffsetsChange.
// This is getting cast to OffsetChange so inheriting from ViewChangeBase isn't working.
// Unknown how it works on WinUI.
internal partial class OffsetsChangeWithAdditionalVelocity : OffsetsChange //: ViewChangeBase
{
	public OffsetsChangeWithAdditionalVelocity(
		Vector2 offsetsVelocity,
		Vector2 anticipatedOffsetsChange,
		Vector2? inertiaDecayRate) : base(0, 0, ScrollPresenterViewKind.Absolute, null)
	{
		OffsetsVelocity = offsetsVelocity;
		AnticipatedOffsetsChange = anticipatedOffsetsChange;
		InertiaDecayRate = inertiaDecayRate;
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_STR_STR, METH_NAME, this,
		// 	TypeLogging::Float2ToString(offsetsVelocity).c_str(),
		// 	TypeLogging::Float2ToString(anticipatedOffsetsChange).c_str(),
		// 	TypeLogging::NullableFloat2ToString(inertiaDecayRate).c_str());
	}

	// ~OffsetsChangeWithAdditionalVelocity()
	// {
	// 	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }

	public Vector2 OffsetsVelocity { get; set; }
	public Vector2 AnticipatedOffsetsChange { get; set; }
	public Vector2? InertiaDecayRate { get; set; }
}
