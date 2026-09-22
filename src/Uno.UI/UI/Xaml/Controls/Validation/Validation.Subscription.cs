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

	private static void OnIsEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		// Generated XAML sets the binding before this attached property, and at that point the element is
		// still parentless with a null DataContext, so registration cannot be gated on it. Pull the current
		// expression here instead of waiting for the next path re-resolution.
		if (sender is Control control
			&& FeatureConfiguration.Validation.IsEnabled
			&& FeatureConfiguration.Validation.GetValidationProperty(control.GetType()) is { } property)
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
		=> FeatureConfiguration.Validation.GetValidationProperty(control.GetType()) == property;

	private static void Synchronize(Control control, BindingExpression expression)
	{
		if (!GetIsEnabled(control))
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

			SetHasErrors(control, false);
			SetErrors(control, Array.Empty<object>());
		}
	}

	private static ValidationState? GetValidationState(Control control)
		=> control.GetValue(ValidationStateProperty) as ValidationState;

	private sealed class ValidationState : IDisposable
	{
		private readonly Control _control;
		private readonly SerialDisposable _subscription = new();

		private BindingExpression? _expression;
		private ManagedWeakReference? _source;
		private string? _propertyName;

		public ValidationState(Control control) => _control = control;

		public EventHandler<DataErrorsChangedEventArgs>? ErrorChanged { get; set; }

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

			var errors = Materialize(source.GetErrors(_propertyName));

			SetErrors(_control, errors);
			SetHasErrors(_control, errors.Length != 0);

			if (args is not null)
			{
				ErrorChanged?.Invoke(_control, args);
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

		private static object[] Materialize(IEnumerable? errors)
		{
			List<object>? materialized = null;

			if (errors is not null)
			{
				foreach (var error in errors)
				{
					(materialized ??= new List<object>()).Add(error);
				}
			}

			// A fresh instance on every synchronization: the dependency property change is what refreshes
			// the binding of the application, and sources commonly hand back the same collection instance.
			return materialized?.ToArray() ?? Array.Empty<object>();
		}
	}
}
