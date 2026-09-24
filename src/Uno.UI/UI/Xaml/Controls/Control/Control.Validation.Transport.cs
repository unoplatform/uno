#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml.Data;
using Uno.UI;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Routes the validation errors of a binding source to the control that bound to it.
/// </summary>
/// <remarks>
/// Plumbing rather than API: the surface an application uses is <see cref="IInputValidationControl"/>.
/// Requires <see cref="Uno.UI.FeatureConfiguration.InputValidation.IsEnabled"/> to be set before the first
/// binding is registered, and the control's type to declare an
/// <see cref="Uno.UI.Xaml.Controls.InputValidationPropertyAttribute"/> or to be registered in
/// <see cref="Uno.UI.FeatureConfiguration.InputValidation.ValidationProperties"/>.
/// </remarks>
public partial class Control
{
	/// <summary>
	/// Holds the per-control subscription. Keyed on the control rather than on the binding expression, which
	/// is replaced without notification whenever the property is rebound.
	/// </summary>
	private static DependencyProperty ValidationSubscriptionProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ValidationSubscription",
			typeof(ValidationSubscription),
			typeof(Control),
			new FrameworkPropertyMetadata(default(ValidationSubscription)));

	private static DependencyProperty ErrorChangedHandlerProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ErrorChangedHandler",
			typeof(EventHandler<DataErrorsChangedEventArgs>),
			typeof(Control),
			new FrameworkPropertyMetadata(default(EventHandler<DataErrorsChangedEventArgs>)));

	private static DependencyProperty HasValidationErrorsChangedHandlerProperty { get; } =
		DependencyProperty.RegisterAttached(
			"HasValidationErrorsChangedHandler",
			typeof(TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs>),
			typeof(Control),
			new FrameworkPropertyMetadata(default(TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs>)));

	private static DependencyProperty ValidationErrorHandlerProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ValidationErrorHandler",
			typeof(TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs>),
			typeof(Control),
			new FrameworkPropertyMetadata(default(TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs>)));

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

	private void OnValidationModeChanged()
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

		// After the errors have settled, so that enabling a control whose source already has errors does not
		// show a cleared state first.
		UpdateValidationStatesInternal();
	}

	private void RaiseHasValidationErrorsChanged(bool newValue)
	{
		if (this is IInputValidationControl validationControl)
		{
			GetHasValidationErrorsChangedHandler()?.Invoke(
				validationControl,
				new HasValidationErrorsChangedEventArgs(newValue));
		}

		UpdateValidationStates();
	}

	private bool IsValidationInputProperty(DependencyProperty property)
		=> FeatureConfiguration.InputValidation.ValidationProperties[GetType()] == property;

	private void SynchronizeValidation(BindingExpression expression)
	{
		if (ValidationParticipant is null)
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

		var subscription = GetValidationSubscription();

		if (subscription is null)
		{
			subscription = new ValidationSubscription(this);
			SetValue(ValidationSubscriptionProperty, subscription);
		}

		subscription.Attach(expression, errorSource, propertyName);
	}

	/// <summary>
	/// Tears the subscription down, but only for the expression that established it: a rebound property
	/// leaves its replaced expression in the binding collection of its owner, still iterated on every
	/// DataContext push, and letting that stale expression clear a live subscription would silently stop
	/// validation.
	/// </summary>
	private void ClearValidationIfOwned(BindingExpression? expression)
	{
		if (GetValidationSubscription() is { } subscription && (expression is null || subscription.Owns(expression)))
		{
			subscription.Dispose();
			ClearValue(ValidationSubscriptionProperty);

			UpdateValidationErrors(sourceErrors: null);
		}
	}

	private ValidationSubscription? GetValidationSubscription()
		=> GetValue(ValidationSubscriptionProperty) as ValidationSubscription;

	/// <summary>
	/// Reconciles the errors of this control with what its source now reports, mutating the collection in
	/// place so that its identity — and any binding to it — survives. Raises one ValidationError per element
	/// added or removed, as WinUI's ValidationErrorsCollection does, then settles HasValidationErrors, whose
	/// changed callback is what drives the visuals.
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
			: (this as IInputValidationControl)?.ValidationErrors;

		if (errors is not null)
		{
			// Drop what the source no longer reports. Matching on the message keeps the instance of an error
			// that is still present, so a binding to it is not churned on every synchronization.
			for (var i = errors.Count - 1; i >= 0; i--)
			{
				if (!incoming.Remove(errors[i].ErrorMessage))
				{
					var removed = errors[i];
					errors.RemoveAt(i);
					RaiseValidationError(InputValidationErrorEventAction.Removed, removed);
				}
			}

			foreach (var message in incoming)
			{
				var added = new InputValidationError(message);
				errors.Add(added);
				RaiseValidationError(InputValidationErrorEventAction.Added, added);
			}
		}

		SetHasValidationErrors(errors is { Count: > 0 });
	}

	/// <summary>
	/// Resolves one of the validation dependency properties on this control's own type.
	/// </summary>
	/// <remarks>
	/// The stand-in for WinUI's <c>GetTargetHasErrorsProperty</c> / <c>GetTargetErrorsProperty</c>, which
	/// switch on a type index over a closed set of four controls. Resolving by name instead keeps
	/// third-party controls working, and costs nothing per call: <see cref="DependencyProperty.GetProperty"/>
	/// is already memoized, and it walks the base-type chain, so <c>CheckBox</c> finds what
	/// <c>ToggleButton</c> registered. No <see cref="Control"/>-owned attached property may therefore be
	/// named after an <see cref="IInputValidationControl"/> member.
	/// </remarks>
	private DependencyProperty? GetValidationProperty(string name)
		=> DependencyProperty.GetProperty(GetType(), name);

	private void SetHasValidationErrors(bool value)
	{
		if (GetValidationProperty(nameof(IInputValidationControl.HasValidationErrors)) is { } property)
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
		=> GetValidationProperty(nameof(IInputValidationControl.ValidationErrors)) is { } property
			? GetValue(property) as IObservableVector<InputValidationError>
			: null;

	private EventHandler<DataErrorsChangedEventArgs>? GetErrorChangedHandler()
		=> GetValue(ErrorChangedHandlerProperty) as EventHandler<DataErrorsChangedEventArgs>;

	private void RaiseErrorChanged(DataErrorsChangedEventArgs args)
		=> GetErrorChangedHandler()?.Invoke(this, args);

	private TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs>? GetHasValidationErrorsChangedHandler()
		=> GetValue(HasValidationErrorsChangedHandlerProperty) as TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs>;

	private TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs>? GetValidationErrorHandler()
		=> GetValue(ValidationErrorHandlerProperty) as TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs>;

	private void RaiseValidationError(InputValidationErrorEventAction action, InputValidationError error)
	{
		if (this is IInputValidationControl sender)
		{
			GetValidationErrorHandler()?.Invoke(sender, new InputValidationErrorEventArgs(action, error));
		}
	}
}
