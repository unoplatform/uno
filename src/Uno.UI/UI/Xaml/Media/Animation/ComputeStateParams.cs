// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ComputeStateParams.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

internal struct ComputeStateParams
{
	public ComputeStateParams()
	{
		time = 0;
		hasTime = false;
		isReversed = false;
		isPaused = false;
		speedRatio = 1;
		pFrameSchedulerNoRef = null;
	}

	public double time;
	public bool hasTime;
	public bool isReversed;

	// In RS1+ we leave paused storyboards in the time manager, so that they keep their targets marked as animating, so that
	// the paused WUC animator stays connected to the tree. The alternative is to detach the WUC animator and use a static
	// value, except Xaml doesn't know what the value is since it's animated independently in the DWM. We don't want paused
	// storyboards to keep requesting ticks, however, so we mark a flag down here and check it before requesting more ticks.
	public bool isPaused;

#if !HAS_UNO
	// TODO Uno: isAncestorWaitingForDCompAnimationCompleted and cannotConvertToCompositionAnimation only apply to
	// composition animations.
#endif

	public float speedRatio;

	public IFrameScheduler? pFrameSchedulerNoRef;
}
