// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ProgressBar.h, tag winui3/release/2.5.1, commit ba3a8d59e

#nullable enable

using Microsoft.UI.Xaml.Shapes;

namespace Microsoft.UI.Xaml.Controls;

partial class ProgressBar
{
	// TODO Uno: WinUI declares m_layoutRootLoadedRevoker (Grid::Loaded_revoker) and
	// m_progressBarIndicatorRevoker (Rectangle::Loaded_revoker) here, but never uses them.

	private Grid? m_layoutRoot;
	private Rectangle? m_determinateProgressBarIndicator;
	private Rectangle? m_indeterminateProgressBarIndicator;
	private Rectangle? m_indeterminateProgressBarIndicator2;

	private const string s_LayoutRootName = "LayoutRoot";
	private const string s_DeterminateProgressBarIndicatorName = "DeterminateProgressBarIndicator";
	private const string s_IndeterminateProgressBarIndicatorName = "IndeterminateProgressBarIndicator";
	private const string s_IndeterminateProgressBarIndicator2Name = "IndeterminateProgressBarIndicator2";
	private const string s_ErrorStateName = "Error";
	private const string s_PausedStateName = "Paused";
	private const string s_IndeterminateStateName = "Indeterminate";
	private const string s_IndeterminateErrorStateName = "IndeterminateError";
	private const string s_IndeterminatePausedStateName = "IndeterminatePaused";
	private const string s_DeterminateStateName = "Determinate";
	private const string s_UpdatingStateName = "Updating";
	private const string s_UpdatingWithErrorStateName = "UpdatingError";
}
