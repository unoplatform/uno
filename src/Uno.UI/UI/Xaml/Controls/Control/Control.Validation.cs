// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference CControl.cpp -- CControl::EnsureValidationVisuals, CControl::EnsureErrors, CControl::DeferErrors, tag winui3/release/1.8.2, commit 45013c4ed

#nullable enable

using Uno.Extras.Input;
using Uno.UI;
using Uno.UI.Xaml.Controls;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

public partial class Control
{
	/// <summary>
	/// Whether, and how, the control participates. A template uses it to reserve the error presenter's space up
	/// front — the compact icon column or the inline row — so the layout does not shift as errors come and go.
	/// </summary>
	internal static class InputValidationEnabledStates
	{
		internal const string ValidationDisabled = nameof(ValidationDisabled);
		internal const string CompactValidationEnabled = nameof(CompactValidationEnabled);
		internal const string InlineValidationEnabled = nameof(InlineValidationEnabled);
	}

	/// <summary>
	/// Whether there are errors to show. A template uses it to reveal the error content inside the space
	/// <see cref="InputValidationEnabledStates"/> reserved.
	/// </summary>
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
	/// counts as enabled, and a control whose type has no validation property is never enabled — which is
	/// what WinUI's type-index switch expresses by returning an unknown property index.
	/// </remarks>
	private bool IsValidationParticipant
		=> ValidationMode != InputValidationMode.Disabled
			&& FeatureConfiguration.InputValidation.ValidationProperties[GetType()] is not null;

	private InputValidationMode ValidationMode
		=> InputValidationProperties.ModeProperty is { } property
			? (InputValidationMode)GetValue(property)
			: InputValidationMode.Disabled;

	private InputValidationKind ValidationKind
		=> InputValidationProperties.KindProperty is { } property
			? (InputValidationKind)GetValue(property)
			: InputValidationKind.Auto;

	private DataTemplate? ValidationErrorTemplate
		=> InputValidationProperties.ErrorTemplateProperty is { } property
			? GetValue(property) as DataTemplate
			: null;

	private bool HasValidationErrors
		=> InputValidationProperties.HasErrorsProperty is { } property
			&& (bool)GetValue(property);

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
		if (!FeatureConfiguration.InputValidation.IsEnabled || !IsValidationParticipant)
		{
			return;
		}

		var hasErrors = HasValidationErrors;

		if (ValidationKind == InputValidationKind.Inline)
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
	/// Applies the validation states from outside this type — from <see cref="FrameworkElement"/>, which as the
	/// base type cannot see a protected member of this one, and from the mode changed handler, which is the
	/// only caller that has to be able to leave the groups.
	/// </summary>
	/// <remarks>
	/// Leaving the groups is the one transition <see cref="UpdateValidationStates"/> cannot make, because it
	/// short-circuits on exactly that condition. Like WinUI's disabled branch, it deliberately leaves the error
	/// states group where it was, and it is outside the feature guard: opting out has to leave the group even
	/// when the global switch was turned off in between.
	/// </remarks>
	internal void UpdateValidationStatesInternal()
	{
		if (IsValidationParticipant)
		{
			UpdateValidationStates();
		}
		else
		{
			GoToState(false, InputValidationEnabledStates.ValidationDisabled);
		}
	}

	/// <summary>
	/// Re-applies the validation visuals once the template exists.
	/// </summary>
	/// <remarks>
	/// Uno: WinUI calls neither from template application, so errors reported before the template is realized
	/// never reach its ErrorPresenter there.
	/// </remarks>
	internal void OnValidationTemplateApplied()
	{
		UpdateValidationStatesInternal();

		if (IsValidationParticipant && HasValidationErrors)
		{
			EnsureErrors();
		}
	}

	private void EnsureErrors()
	{
		if (!FeatureConfiguration.InputValidation.IsEnabled || !IsValidationParticipant)
		{
			return;
		}

		// When an error occurs, we want to undefer the error presenter and load the error template. We want to hook up
		// the appropriate data context so that controls can use {Binding} in their ErrorTemplates
		if (GetTemplateChild("ErrorPresenter") is ContentPresenter errorPresenter)
		{
			EnsureValidationVisuals();
			if (ValidationErrorTemplate is { } errorTemplate
				&& errorTemplate.LoadContent() is FrameworkElement loadedContent)
			{
				loadedContent.DataContext = this;

				object content = loadedContent;

				if (ValidationKind != InputValidationKind.Inline)
				{
					// We aren't showing errors inline, get the default compact template and set the errors as the content of the
					// tooltip. We then use the tree created from the compact template as the content for the presenter
					if (ResourceResolver.ResolveTopLevelResource("DefaultCompactErrorIconTemplate") is DataTemplate compactTemplate
						&& compactTemplate.LoadContent() is { } iconContent)
					{
						if (ToolTipService.GetToolTip(iconContent) is ContentControl toolTip)
						{
							toolTip.Content = loadedContent;
						}

						content = iconContent;
					}
				}

				errorPresenter.Content = content;
			}
		}
	}

	private void DeferErrors()
	{
		// Uno: there is no TryDefer, so a realized presenter stays realized — the outcome WinUI already accepts for
		// a template without x:Load — and the InputValidationErrorStates group is what hides it. The presenter is
		// not looked up either: in Uno, that would realize it.
		EnsureValidationVisuals();
	}

	/// <summary>
	/// Backs <c>Validation.Kind</c>'s changed handler.
	/// </summary>
	internal void OnValidationKindChanged() => UpdateValidationStates();

	/// <summary>
	/// Backs <c>Validation.ErrorTemplate</c>'s changed handler.
	/// </summary>
	internal void OnValidationErrorTemplateChanged()
	{
		if (IsValidationParticipant && HasValidationErrors)
		{
			EnsureErrors();
		}
	}

	/// <summary>
	/// Backs <c>Validation.Errors</c>: the collection is created on first read, and its identity then stays
	/// stable for the life of the control.
	/// </summary>
	internal IObservableVector<InputValidationError> GetOrCreateValidationErrors(DependencyProperty property)
	{
		if (GetValue(property) is not ValidationErrorsCollection errors)
		{
			errors = new ValidationErrorsCollection();
			SetValue(property, errors);
		}

		return errors;
	}
}
