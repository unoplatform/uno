// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference storyboard.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Storyboard
{
	internal override void OnBegin() => BeginPrivate(/* fIsTopLevel */ false);

	internal override bool IsInActiveState() => !m_fIsPaused && base.IsInActiveState();

	internal override bool IsInStoppedState() => m_fIsStopped && base.IsInStoppedState();

	internal void RequestAutoComplete(bool fValue) => m_fAutoComplete = (fValue && !m_fIsEssential);

	internal bool m_fIsEssential;

	private bool m_fIsStopped = true;
	private bool m_fIsPaused;
	private bool m_fIsResuming;
	private bool m_fIsBeginning;
	private bool m_fIsSeeking;
	private bool m_fAutoComplete;

	private double m_lastParentTime = double.MaxValue;  // unset sentinel value
	private double m_rPendingSeekTime = double.MaxValue;
	private double m_rTimeDelta = double.MaxValue;
}
