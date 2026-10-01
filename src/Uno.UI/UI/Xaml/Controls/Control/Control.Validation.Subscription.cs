#nullable enable

using System;
using System.ComponentModel;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Dispatching;
using Microsoft.UI.Xaml.Data;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class Control
{
	/// <summary>
	/// The validation state of one control, created on first use so that a control which never validates pays
	/// only the field. The handlers outlive <see cref="Subscription"/>, which comes and goes with the binding.
	/// </summary>
	/// <remarks>
	/// The subscription is keyed on the control rather than on the binding expression, which is replaced
	/// without notification whenever the property is rebound.
	/// </remarks>
	private sealed class ValidationState
	{
		public ValidationSubscription? Subscription;
		public EventHandler<DataErrorsChangedEventArgs>? ErrorChanged;
		public TypedEventHandler<IInputValidationControl, HasValidationErrorsChangedEventArgs>? HasValidationErrorsChanged;
		public TypedEventHandler<IInputValidationControl, InputValidationErrorEventArgs>? ValidationError;
	}

	/// <summary>
	/// The live subscription to one binding source, held by the control for as long as that binding stands.
	/// </summary>
	private sealed class ValidationSubscription : IDisposable
	{
		private readonly Control _control;
		private readonly SerialDisposable _subscription = new();

		private BindingExpression? _expression;
		private ManagedWeakReference? _source;
		private string? _propertyName;

		public ValidationSubscription(Control control) => _control = control;

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

			_control.UpdateValidationErrors(source.GetErrors(_propertyName));

			if (args is not null)
			{
				_control.RaiseErrorChanged(args);
			}
		}

		/// <summary>
		/// Subscribes without letting the source root the control: the event closure holds only a weak
		/// reference to this subscription, which the control keeps alive through its validation state, and
		/// the disposer re-resolves the source rather than capturing it.
		/// </summary>
		private static IDisposable Subscribe(INotifyDataErrorInfo source, string propertyName, ValidationSubscription state)
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

				if (!stateWeak.IsDisposed && stateWeak.Target is ValidationSubscription target)
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
