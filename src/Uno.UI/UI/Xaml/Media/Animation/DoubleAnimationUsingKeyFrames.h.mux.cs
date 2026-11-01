// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference DoubleAnimationUsingKeyFrames.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

partial class DoubleAnimationUsingKeyFrames
{
	public DoubleAnimationUsingKeyFrames()
	{
#if HAS_UNO
		KeyFrames = new DoubleKeyFrameCollection(this, isAutoPropertyInheritanceEnabled: false);
#endif
	}

	private protected override bool m_fUsesKeyFrames => true;

#if !HAS_UNO
	// TODO Uno: NOT PORTED - MakeCompositionKeyFrameAnimationVirtual: composition animations.
#endif
}
