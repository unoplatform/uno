// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference CControl.cpp -- CControl::EnsureValidationVisuals, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

using System;
using System.ComponentModel;
using Uno.UI;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

public partial class Control
{
	internal static class InputValidationEnabledStates
	{
		internal const string ValidationDisabled = nameof(ValidationDisabled);
		internal const string CompactValidationEnabled = nameof(CompactValidationEnabled);
		internal const string InlineValidationEnabled = nameof(InlineValidationEnabled);
	}

	internal static class InputValidationErrorStates
	{
		internal const string CompactErrors = nameof(CompactErrors);
		internal const string InlineErrors = nameof(InlineErrors);
		internal const string ErrorsCleared = nameof(ErrorsCleared);
	}

	/// <summary>
	/// Whether this control participates in input validation.
	/// </summary>
	/// <remarks>
	/// Mirrors <c>CControl::IsValidationEnabled</c>: every mode but <see cref="InputValidationMode.Disabled"/>
	/// counts as enabled, and a control that does not implement the interface is never enabled — which is
	/// what WinUI's type-index switch expresses by returning an unknown property index.
	/// </remarks>
	private bool IsValidationEnabled
		=> this is IInputValidationControl { InputValidationMode: not InputValidationMode.Disabled };

	/// <summary>
	/// Applies the <see cref="InputValidationEnabledStates"/> and <see cref="InputValidationErrorStates"/> visual state groups.
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
			GoToState(false, InputValidationEnabledStates.InlineValidationEnabled);
			GoToState(false, hasErrors ? InputValidationErrorStates.InlineErrors : InputValidationErrorStates.ErrorsCleared);
		}
		else
		{
			// Auto resolves to Compact, as it does in WinUI: ShowErrorsInline is "kind == Inline", and
			// nothing ever maps Auto to anything else.
			GoToState(false, InputValidationEnabledStates.CompactValidationEnabled);
			GoToState(false, hasErrors ? InputValidationErrorStates.CompactErrors : InputValidationErrorStates.ErrorsCleared);
		}
	}

	/// <summary>
	/// Reaches <see cref="UpdateValidationStates"/> from <see cref="FrameworkElement"/>, which as the base
	/// type cannot see a protected member of this one.
	/// </summary>
	internal void UpdateValidationStatesInternal() => UpdateValidationStates();

	/// <summary>
	/// Leaves the groups when the control stops participating — the one transition
	/// <see cref="UpdateValidationStates"/> cannot make, because it short-circuits on exactly that condition.
	/// </summary>
	/// <remarks>
	/// Like WinUI's disabled branch, this deliberately leaves the error states group where it was.
	/// </remarks>
	private void ClearValidationStates() => GoToState(false, InputValidationEnabledStates.ValidationDisabled);

	/// <summary>
	/// The changed callback a participating control registers its InputValidationMode with.
	/// </summary>
	/// <remarks>
	/// Protected rather than internal because a control defined outside Uno.UI participates by registering
	/// these same dependency properties itself. Such a control registers with <see cref="PropertyMetadata"/>:
	/// the <see cref="FrameworkPropertyMetadata"/> overload taking a value and a callback is internal.
	/// </remarks>
	protected static void OnInputValidationModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		if (sender is Control control)
		{
			control.OnValidationModeChanged();
		}
	}

	/// <inheritdoc cref="OnInputValidationModeChanged"/>
	protected static void OnInputValidationKindChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		if (sender is Control control)
		{
			control.UpdateValidationStates();
		}
	}

	/// <inheritdoc cref="OnInputValidationModeChanged"/>
	/// <remarks>
	/// Raising HasValidationErrorsChanged from here rather than from where the errors are reconciled is what
	/// makes it fire once per transition rather than once per synchronization.
	/// </remarks>
	protected static void OnHasValidationErrorsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		if (sender is Control control)
		{
			control.RaiseHasValidationErrorsChanged((bool)args.NewValue);
		}
	}

	/// <summary>
	/// Backs <see cref="IInputValidationControl.ValidationErrors"/>: the collection is created on first read,
	/// and its identity then stays stable for the life of the control.
	/// </summary>
	/// <param name="property">The ValidationErrors dependency property the control registered.</param>
	protected IObservableVector<InputValidationError> GetOrCreateValidationErrors(DependencyProperty property)
	{
		if (GetValue(property) is not ValidationErrorsCollection errors)
		{
			errors = new ValidationErrorsCollection();
			SetValue(property, errors);
		}

		return errors;
	}

	/// <summary>
	/// Backs <see cref="IInputValidationControl.HasValidationErrorsChanged"/>, so that no control needs
	/// storage of its own.
	/// </summary>
	protected void AddHasValidationErrorsChangedHandler(TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs> handler)
		=> SetValue(
			HasValidationErrorsChangedHandlerProperty,
			Delegate.Combine(GetHasValidationErrorsChangedHandler(), handler));

	/// <inheritdoc cref="AddHasValidationErrorsChangedHandler"/>
	protected void RemoveHasValidationErrorsChangedHandler(TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs> handler)
		=> SetValue(
			HasValidationErrorsChangedHandlerProperty,
			Delegate.Remove(GetHasValidationErrorsChangedHandler(), handler));

	/// <summary>
	/// Backs <see cref="IInputValidationControl.ValidationError"/>.
	/// </summary>
	protected void AddValidationErrorHandler(TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs> handler)
		=> SetValue(
			ValidationErrorHandlerProperty,
			Delegate.Combine(GetValidationErrorHandler(), handler));

	/// <inheritdoc cref="AddValidationErrorHandler"/>
	protected void RemoveValidationErrorHandler(TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs> handler)
		=> SetValue(
			ValidationErrorHandlerProperty,
			Delegate.Remove(GetValidationErrorHandler(), handler));

	/// <summary>
	/// Backs <see cref="IInputValidationControl.ErrorChanged"/>. Kept apart from the subscription state, which
	/// comes and goes with the binding while handlers must survive a rebind.
	/// </summary>
	protected void AddErrorChangedHandler(EventHandler<DataErrorsChangedEventArgs> handler)
		=> SetValue(
			ErrorChangedHandlerProperty,
			Delegate.Combine(GetErrorChangedHandler(), handler));

	/// <inheritdoc cref="AddErrorChangedHandler"/>
	protected void RemoveErrorChangedHandler(EventHandler<DataErrorsChangedEventArgs> handler)
		=> SetValue(
			ErrorChangedHandlerProperty,
			Delegate.Remove(GetErrorChangedHandler(), handler));
}
