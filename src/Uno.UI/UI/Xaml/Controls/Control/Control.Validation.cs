// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference CControl.cpp -- CControl::EnsureValidationVisuals, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

using Uno.UI;
using Validation = Uno.UI.Xaml.Controls.Validation;

namespace Microsoft.UI.Xaml.Controls;

public partial class Control
{
	internal const string ValidationDisabledState = "ValidationDisabled";
	internal const string CompactValidationEnabledState = "CompactValidationEnabled";
	internal const string InlineValidationEnabledState = "InlineValidationEnabled";
	internal const string CompactErrorsState = "CompactErrors";
	internal const string InlineErrorsState = "InlineErrors";
	internal const string ErrorsClearedState = "ErrorsCleared";

	/// <summary>
	/// Applies the InputValidationEnabledStates and InputValidationErrorStates visual state groups.
	/// </summary>
	/// <remarks>
	/// Call it from <c>ChangeVisualState</c> in any control that participates in input validation. It is not
	/// virtual: a control outside this assembly cannot override <c>ChangeVisualState</c>, but it can call this
	/// from wherever it does drive its states.
	/// <para>
	/// The framework already applies the states when the errors change, when participation changes and when
	/// the template is realized, so a call from a control is a re-application rather than the mechanism.
	/// </para>
	/// </remarks>
	protected void UpdateValidationStates()
	{
		// Validation is opt-in per control, so for nearly every control this is the whole method — and it
		// runs on the visual-state path and on every template application.
		if (!FeatureConfiguration.InputValidation.IsEnabled
			|| this is not IInputValidationControl { InputValidationMode: not InputValidationMode.Disabled } participant)
		{
			return;
		}

		var hasErrors = participant.HasValidationErrors;

		if (participant.InputValidationKind == InputValidationKind.Inline)
		{
			GoToState(false, InlineValidationEnabledState);
			GoToState(false, hasErrors ? InlineErrorsState : ErrorsClearedState);
		}
		else
		{
			// Auto resolves to Compact, as it does in WinUI: ShowErrorsInline is `kind == Inline`, and
			// nothing ever maps Auto to anything else.
			GoToState(false, CompactValidationEnabledState);
			GoToState(false, hasErrors ? CompactErrorsState : ErrorsClearedState);
		}
	}

	internal void UpdateValidationStatesInternal() => UpdateValidationStates();

	/// <summary>
	/// Leaves the groups when the control stops participating — the one transition
	/// <see cref="UpdateValidationStates"/> cannot make, because it short-circuits on exactly that condition.
	/// </summary>
	/// <remarks>
	/// Like WinUI's disabled branch, this deliberately leaves the error states group where it was.
	/// </remarks>
	internal void ClearValidationStates() => GoToState(false, ValidationDisabledState);
}
