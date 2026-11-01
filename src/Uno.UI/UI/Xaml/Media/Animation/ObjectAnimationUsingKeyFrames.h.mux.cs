// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ObjectAnimationUsingKeyFrames.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

partial class ObjectAnimationUsingKeyFrames
{
	private protected override bool m_fUsesKeyFrames => true;

	private protected override void ComputeToUsingFromAndBy() { }

	private object? m_vFrom;
	private object? m_vTo;
	private object? m_vBy;
	private object? m_vBaseValue;
	private object? m_vNonAnimatedBaseValue;
	private object? m_vCurrentValue;
}
