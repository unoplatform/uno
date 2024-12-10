// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Windows.UI.Xaml.Controls;

public partial class ScrollingZoomOptions
{
	internal const ScrollingAnimationMode s_defaultAnimationMode = ScrollingAnimationMode.Auto;
	internal const ScrollingSnapPointsMode s_defaultSnapPointsMode = ScrollingSnapPointsMode.Default;

	public ScrollingZoomOptions(
		ScrollingAnimationMode animationMode)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR, METH_NAME, this,
		// 	TypeLogging::AnimationModeToString(animationMode).c_str());

		AnimationMode = animationMode;
	}

	public ScrollingZoomOptions(
		ScrollingAnimationMode animationMode,
		ScrollingSnapPointsMode snapPointsMode)
	{
		// SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_STR, METH_NAME, this,
		// 	TypeLogging::AnimationModeToString(animationMode).c_str(),
		// 	TypeLogging::SnapPointsModeToString(snapPointsMode).c_str());

		AnimationMode = animationMode;
		SnapPointsMode = snapPointsMode;
	}

	public ScrollingAnimationMode AnimationMode { get; set; } = s_defaultAnimationMode;

	public ScrollingSnapPointsMode SnapPointsMode { get; set; } = s_defaultSnapPointsMode;

	// ~ScrollingZoomOptions()
	// {
	//     SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	// }
}
