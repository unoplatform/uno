#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Dispatching;
using Windows.Foundation;

namespace Uno.UI.Xaml.Controls;

public static partial class Validation
{
	/// <summary>
	/// Holds the per-control subscription. Keyed on the control rather than on the binding expression, which
	/// is replaced without notification whenever the property is rebound.
	/// </summary>
	private static DependencyProperty ValidationStateProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ValidationState",
			typeof(ValidationState),
			typeof(Validation),
			new FrameworkPropertyMetadata(default(ValidationState)));

	/// <summary>
	/// Marks an expression that targets the validation property of its owner. Both SetBindingInternal
	/// overloads converge here, and a ResourceBinding never reaches it.
	/// </summary>
	internal static void OnBindingSet(DependencyObject owner, DependencyProperty property, BindingExpression expression)
	{
		if (owner is Control control && IsValidationProperty(control, property))
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
			Synchronize(control, expression);
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
			ClearIfOwned(control, expression);
		}
	}

	private static void OnInputValidationModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		// Generated XAML sets the binding before this attached property, and at that point the element is
		// still parentless with a null DataContext, so registration cannot be gated on it. Pull the current
		// expression here instead of waiting for the next path re-resolution.
		if (sender is Control control
			&& FeatureConfiguration.InputValidation.IsEnabled
			&& FeatureConfiguration.InputValidation.ValidationProperties[control.GetType()] is { } property)
		{
			if (control.GetBindingExpression(property) is { } expression)
			{
				expression.IsValidationSource = true;
				Synchronize(control, expression);
			}
			else
			{
				ClearIfOwned(control, expression: null);
			}
		}
	}

	private static bool IsValidationProperty(Control control, DependencyProperty property)
		=> FeatureConfiguration.InputValidation.ValidationProperties[control.GetType()] == property;

	private static void Synchronize(Control control, BindingExpression expression)
	{
		if (!IsValidationEnabled(control))
		{
			ClearIfOwned(control, expression);
			return;
		}

		var (source, propertyName) = expression.GetValidationLeaf();

		if (source is not INotifyDataErrorInfo errorSource || string.IsNullOrEmpty(propertyName))
		{
			ClearIfOwned(control, expression);
			return;
		}

		var state = GetValidationState(control);

		if (state is null)
		{
			state = new ValidationState(control);
			control.SetValue(ValidationStateProperty, state);
		}

		state.Attach(expression, errorSource, propertyName);
	}

	/// <summary>
	/// Tears the subscription down, but only for the expression that established it: a rebound property
	/// leaves its replaced expression in the binding collection of its owner, still iterated on every
	/// DataContext push, and letting that stale expression clear a live subscription would silently stop
	/// validation.
	/// </summary>
	private static void ClearIfOwned(Control control, BindingExpression? expression)
	{
		if (GetValidationState(control) is { } state && (expression is null || state.Owns(expression)))
		{
			state.Dispose();
			control.ClearValue(ValidationStateProperty);

			UpdateErrors(control, sourceErrors: null);
		}
	}

	private static ValidationState? GetValidationState(Control control)
		=> control.GetValue(ValidationStateProperty) as ValidationState;

	/// <summary>
	/// Reconciles the errors of <paramref name="control"/> with what its source now reports, mutating the
	/// collection in place so that its identity — and any binding to it — survives. Raises one
	/// ValidationError per element added or removed, as WinUI's ValidationErrorsCollection does, then settles
	/// HasErrors, whose changed callback is what drives the visuals.
	/// </summary>
	private static void UpdateErrors(Control control, IEnumerable? sourceErrors)
	{
		var incoming = new List<string>();

		if (sourceErrors is not null)
		{
			foreach (var error in sourceErrors)
			{
				incoming.Add(error?.ToString() ?? string.Empty);
			}
		}

		var errors = incoming.Count == 0 ? TryGetErrors(control) : (ValidationErrorsCollection)GetErrors(control);

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
					RaiseValidationError(control, InputValidationErrorEventAction.Removed, removed);
				}
			}

			foreach (var message in incoming)
			{
				var added = new InputValidationError(message);
				errors.Add(added);
				RaiseValidationError(control, InputValidationErrorEventAction.Added, added);
			}
		}

		SetHasErrors(control, errors is { Count: > 0 });
	}

	/// <summary>
	/// Backs <see cref="IInputValidationControl.ErrorChanged"/> for every participating control, so that no
	/// control needs storage of its own. Kept apart from the subscription state, which comes and goes with
	/// the binding while handlers must survive a rebind.
	/// </summary>
	private static DependencyProperty ErrorChangedHandlerProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ErrorChangedHandler",
			typeof(EventHandler<DataErrorsChangedEventArgs>),
			typeof(Validation),
			new FrameworkPropertyMetadata(default(EventHandler<DataErrorsChangedEventArgs>)));

	internal static void AddErrorChangedHandler(Control control, EventHandler<DataErrorsChangedEventArgs> handler)
		=> control.SetValue(
			ErrorChangedHandlerProperty,
			Delegate.Combine(GetErrorChangedHandler(control), handler));

	internal static void RemoveErrorChangedHandler(Control control, EventHandler<DataErrorsChangedEventArgs> handler)
		=> control.SetValue(
			ErrorChangedHandlerProperty,
			Delegate.Remove(GetErrorChangedHandler(control), handler));

	private static EventHandler<DataErrorsChangedEventArgs>? GetErrorChangedHandler(Control control)
		=> control.GetValue(ErrorChangedHandlerProperty) as EventHandler<DataErrorsChangedEventArgs>;

	private static void RaiseErrorChanged(Control control, DataErrorsChangedEventArgs args)
		=> GetErrorChangedHandler(control)?.Invoke(control, args);

	/// <summary>
	/// Backs <see cref="IInputValidationControl.HasValidationErrorsChanged"/> and
	/// <see cref="IInputValidationControl.ValidationError"/>, on the same no-per-control-storage principle as
	/// <see cref="ErrorChangedHandlerProperty"/>.
	/// </summary>
	private static DependencyProperty HasValidationErrorsChangedHandlerProperty { get; } =
		DependencyProperty.RegisterAttached(
			"HasValidationErrorsChangedHandler",
			typeof(TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs>),
			typeof(Validation),
			new FrameworkPropertyMetadata(default(TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs>)));

	private static DependencyProperty ValidationErrorHandlerProperty { get; } =
		DependencyProperty.RegisterAttached(
			"ValidationErrorHandler",
			typeof(TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs>),
			typeof(Validation),
			new FrameworkPropertyMetadata(default(TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs>)));

	internal static void AddHasValidationErrorsChangedHandler(Control control, TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs> handler)
		=> control.SetValue(
			HasValidationErrorsChangedHandlerProperty,
			Delegate.Combine(GetHasValidationErrorsChangedHandler(control), handler));

	internal static void RemoveHasValidationErrorsChangedHandler(Control control, TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs> handler)
		=> control.SetValue(
			HasValidationErrorsChangedHandlerProperty,
			Delegate.Remove(GetHasValidationErrorsChangedHandler(control), handler));

	private static TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs>? GetHasValidationErrorsChangedHandler(Control control)
		=> control.GetValue(HasValidationErrorsChangedHandlerProperty) as TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs>;

	internal static void AddValidationErrorHandler(Control control, TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs> handler)
		=> control.SetValue(
			ValidationErrorHandlerProperty,
			Delegate.Combine(GetValidationErrorHandler(control), handler));

	internal static void RemoveValidationErrorHandler(Control control, TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs> handler)
		=> control.SetValue(
			ValidationErrorHandlerProperty,
			Delegate.Remove(GetValidationErrorHandler(control), handler));

	private static TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs>? GetValidationErrorHandler(Control control)
		=> control.GetValue(ValidationErrorHandlerProperty) as TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs>;

	private static void RaiseValidationError(Control control, InputValidationErrorEventAction action, InputValidationError error)
	{
		if (control is IInputValidationControl sender)
		{
			GetValidationErrorHandler(control)?.Invoke(sender, new InputValidationErrorEventArgs(action, error));
		}
	}

	/// <summary>
	/// Raised from the HasErrors changed callback, so that it fires once per transition rather than once per
	/// synchronization.
	/// </summary>
	private static void OnHasErrorsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		if (sender is Control control and IInputValidationControl validationControl)
		{
			GetHasValidationErrorsChangedHandler(control)?.Invoke(
				validationControl,
				new HasValidationErrorsChangedEventArgs((bool)args.NewValue));
		}
	}

	private sealed class ValidationState : IDisposable
	{
		private readonly Control _control;
		private readonly SerialDisposable _subscription = new();

		private BindingExpression? _expression;
		private ManagedWeakReference? _source;
		private string? _propertyName;

		public ValidationState(Control control) => _control = control;

		public bool Owns(BindingExpression expression) => ReferenceEquals(_expression, expression);

		public void Attach(BindingExpression expression, INotifyDataErrorInfo source, string propertyName)
		{
			_expression = expression;

			if (!ReferenceEquals(_source?.Target, source)
				|| !string.Equals(_propertyName, propertyName, StringComparison.Ordinal))
			{
				_propertyName = propertyName;
				_source = WeakReferencePool.RentWeakReference(this, source);
				_subscription.Disposable = Subscribe(source, propertyName, this);
			}

			Synchronize(args: null);
		}

		public void Dispose()
		{
			_subscription.Dispose();
			_expression = null;
			_source = null;
			_propertyName = null;
		}

		private void OnErrorsChanged(DataErrorsChangedEventArgs args)
		{
			if (NativeDispatcher.Main.HasThreadAccess)
			{
				Synchronize(args);
			}
			else
			{
				// A view model may raise ErrorsChanged from any thread; the property system is UI-thread bound.
				_control.DispatcherQueue.TryEnqueue(() => Synchronize(args));
			}
		}

		private void Synchronize(DataErrorsChangedEventArgs? args)
		{
			if (_source?.Target is not INotifyDataErrorInfo source)
			{
				return;
			}

			UpdateErrors(_control, source.GetErrors(_propertyName));

			if (args is not null)
			{
				RaiseErrorChanged(_control, args);
			}
		}

		/// <summary>
		/// Subscribes without letting the source root the control: the event closure holds only a weak
		/// reference to this state, which the control keeps alive through its attached property, and the
		/// disposer re-resolves the source rather than capturing it.
		/// </summary>
		private static IDisposable Subscribe(INotifyDataErrorInfo source, string propertyName, ValidationState state)
		{
			var stateWeak = WeakReferencePool.RentWeakReference(null, state);
			var sourceWeak = WeakReferencePool.RentWeakReference(null, source);

			EventHandler<DataErrorsChangedEventArgs> handler = (_, args) =>
			{
				// An empty property name means "all properties", the convention INotifyPropertyChanged uses.
				if (!string.IsNullOrEmpty(args.PropertyName)
					&& !string.Equals(args.PropertyName, propertyName, StringComparison.Ordinal))
				{
					return;
				}

				if (!stateWeak.IsDisposed && stateWeak.Target is ValidationState target)
				{
					target.OnErrorsChanged(args);
				}
			};

			source.ErrorsChanged += handler;

			return Disposable.Create(() =>
			{
				if (sourceWeak.Target is INotifyDataErrorInfo that)
				{
					that.ErrorsChanged -= handler;
				}

				WeakReferencePool.ReturnWeakReference(null, stateWeak);
				WeakReferencePool.ReturnWeakReference(null, sourceWeak);
			});
		}
	}
}
