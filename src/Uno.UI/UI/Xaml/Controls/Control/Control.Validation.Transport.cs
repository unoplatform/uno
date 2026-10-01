#nullable enable

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Uno.Extras.Input;
using Uno.UI;
using Uno.UI.Xaml.Controls;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Routes the validation errors of a binding source to the control that bound to it.
/// </summary>
/// <remarks>
/// Plumbing rather than API: the surface an application uses is the <c>Uno.Extras.Input.Validation</c>
/// attached properties of Uno.UI.Extras.
/// Requires <see cref="Uno.UI.FeatureConfiguration.InputValidation.IsEnabled"/> to be set before the first
/// binding is registered, and the control's type to declare an
/// <see cref="Uno.UI.Xaml.Controls.InputValidationPropertyAttribute"/> or to be registered in
/// <see cref="Uno.UI.FeatureConfiguration.InputValidation.ValidationProperties"/>.
/// </remarks>
public partial class Control
{
	private ValidationSubscription? _validationSubscription;

	/// <summary>
	/// Marks an expression that targets the validation property of its owner. Both SetBindingInternal
	/// overloads converge here, and a ResourceBinding never reaches it.
	/// </summary>
	internal static void OnValidationBindingSet(DependencyObject owner, DependencyProperty property, BindingExpression expression)
	{
		if (owner is Control control && control.IsValidationInputProperty(property))
		{
			expression.IsValidationSource = true;
		}
	}

	/// <summary>
	/// Called on every re-resolution of a validation binding path. That is also the only signal that
	/// survives a source whose initial value equals the target property default.
	/// </summary>
	internal static void OnValidationSourceChanged(BindingExpression expression)
	{
		if (expression.ViewReference?.Target is Control control)
		{
			control.SynchronizeValidation(expression);
		}
	}

	/// <summary>
	/// Called when a validation binding is torn down, which happens silently: the expression is disposed
	/// before it could raise anything.
	/// </summary>
	internal static void OnValidationBindingCleared(BindingExpression expression)
	{
		if (expression.ViewReference?.Target is Control control)
		{
			control.ClearValidationIfOwned(expression);
		}
	}

	/// <summary>
	/// Backs <c>Validation.Mode</c>'s changed handler.
	/// </summary>
	internal void OnValidationModeChanged()
	{
		// Generated XAML sets the binding before this property, and at that point the element is still
		// parentless with a null DataContext, so registration cannot be gated on it. Pull the current
		// expression here instead of waiting for the next path re-resolution.
		if (FeatureConfiguration.InputValidation.IsEnabled
			&& FeatureConfiguration.InputValidation.ValidationProperties[GetType()] is { } property)
		{
			if (GetBindingExpression(property) is { } expression)
			{
				expression.IsValidationSource = true;
				SynchronizeValidation(expression);
			}
			else
			{
				ClearValidationIfOwned(expression: null);
			}
		}

		if (HasValidationErrors)
		{
			if (IsValidationParticipant)
			{
				EnsureErrors();
			}
			else
			{
				DeferErrors();
			}
		}

		// After the errors have settled, so that enabling a control whose source already has errors does not
		// show a cleared state first.
		UpdateValidationStatesInternal();
	}

	/// <summary>
	/// Backs <c>Validation.HasErrors</c>'s changed handler.
	/// </summary>
	internal void OnHasValidationErrorsChanged(bool newValue)
	{
		UpdateValidationStates();

		// The equivalent of WinUI's RaiseValidationErrorEvent check, which loads the error template on the first
		// error and defers it on the last.
		if (!newValue)
		{
			DeferErrors();
		}
		else if (IsValidationParticipant)
		{
			EnsureErrors();
		}
	}

	private bool IsValidationInputProperty(DependencyProperty property)
		=> FeatureConfiguration.InputValidation.ValidationProperties[GetType()] == property;

	private void SynchronizeValidation(BindingExpression expression)
	{
		if (!IsValidationParticipant)
		{
			ClearValidationIfOwned(expression);
			return;
		}

		var (source, propertyName) = expression.GetValidationLeaf();

		if (source is not INotifyDataErrorInfo errorSource || string.IsNullOrEmpty(propertyName))
		{
			ClearValidationIfOwned(expression);
			return;
		}

		_validationSubscription ??= new ValidationSubscription(this);
		_validationSubscription.Attach(expression, errorSource, propertyName);
	}

	/// <summary>
	/// Tears the subscription down, but only for the expression that established it: a rebound property
	/// leaves its replaced expression in the binding collection of its owner, still iterated on every
	/// DataContext push, and letting that stale expression clear a live subscription would silently stop
	/// validation.
	/// </summary>
	private void ClearValidationIfOwned(BindingExpression? expression)
	{
		if (_validationSubscription is { } subscription
			&& (expression is null || subscription.Owns(expression)))
		{
			subscription.Dispose();
			_validationSubscription = null;

			UpdateValidationErrors(sourceErrors: null);
		}
	}

	/// <summary>
	/// Reconciles the errors of this control with what its source now reports, mutating the collection in
	/// place so that its identity — and any binding to it — survives, then settles HasErrors, whose changed
	/// handler is what drives the visuals.
	/// </summary>
	private void UpdateValidationErrors(IEnumerable? sourceErrors)
	{
		var incoming = new List<string>();

		if (sourceErrors is not null)
		{
			foreach (var error in sourceErrors)
			{
				incoming.Add(error?.ToString() ?? string.Empty);
			}
		}

		var errors = incoming.Count == 0
			? TryGetValidationErrors()
			: InputValidationProperties.ErrorsProperty is { } property ? GetOrCreateValidationErrors(property) : null;

		if (errors is not null)
		{
			// Drop what the source no longer reports. Matching on the message keeps the instance of an error
			// that is still present, so a binding to it is not churned on every synchronization.
			for (var i = errors.Count - 1; i >= 0; i--)
			{
				if (!incoming.Remove(errors[i].ErrorMessage))
				{
					errors.RemoveAt(i);
				}
			}

			foreach (var message in incoming)
			{
				errors.Add(new InputValidationError(message));
			}
		}

		SetHasValidationErrors(errors is { Count: > 0 });
	}

	private void SetHasValidationErrors(bool value)
	{
		if (InputValidationProperties.HasErrorsProperty is { } property)
		{
			SetValue(property, value);
		}
	}

	/// <summary>
	/// The errors of this control, or null when it has never reported one — so that a control which never
	/// reports an error never allocates a collection. Mirrors WinUI reading the equivalent property with
	/// <c>CheckOnDemandProperty</c>, which deliberately does not force-create.
	/// </summary>
	private IObservableVector<InputValidationError>? TryGetValidationErrors()
		=> InputValidationProperties.ErrorsProperty is { } property
			? GetValue(property) as IObservableVector<InputValidationError>
			: null;
}
