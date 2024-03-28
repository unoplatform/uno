// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference ScrollingScrollOptions.cpp, ScrollingScrollOptions.h, tag winui3/release/1.4.2

namespace Microsoft.UI.Xaml.Controls;

public partial class ScrollingScrollOptions
{
	internal const ScrollingAnimationMode s_defaultAnimationMode = ScrollingAnimationMode.Auto;
	internal const ScrollingSnapPointsMode s_defaultSnapPointsMode = ScrollingSnapPointsMode.Default;

	public ScrollingScrollOptions(
		ScrollingAnimationMode animationMode)
	{
		//SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR, METH_NAME, this,
		//		TypeLogging::AnimationModeToString(animationMode).c_str());

		AnimationMode = animationMode;
	}

	public ScrollingScrollOptions(
		ScrollingAnimationMode animationMode,
		ScrollingSnapPointsMode snapPointsMode)
	{
		//SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH_STR_STR, METH_NAME, this,
		//	TypeLogging::AnimationModeToString(animationMode).c_str(),
		//	TypeLogging::SnapPointsModeToString(snapPointsMode).c_str());

		AnimationMode = animationMode;
		SnapPointsMode = snapPointsMode;
	}

	//~ScrollingScrollOptions()
	//{
	//	SCROLLPRESENTER_TRACE_VERBOSE(nullptr, TRACE_MSG_METH, METH_NAME, this);
	//}

	public ScrollingAnimationMode AnimationMode { get; set; } = s_defaultAnimationMode;
	public ScrollingSnapPointsMode SnapPointsMode { get; set; } = s_defaultSnapPointsMode;
}
