// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference components/animation/Storyboard.cpp, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Storyboard
{
#if !HAS_UNO
	// TODO Uno: NOT PORTED - PauseDCompAnimationsOnSuspend, ResumeDCompAnimationsOnResume and MakeCompositionAnimationVirtual:
	// composition animations.
#endif

	internal bool IsPaused() => m_fIsPaused;
}
