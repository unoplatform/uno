#nullable enable

using System;
using System.ComponentModel;
using Uno.Disposables;
using Uno.UI.DataBinding;
using Uno.UI.Dispatching;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls;

public partial class Control
{
	/// <summary>
	/// The live subscription to one binding source, held by the control for as long as that binding stands.
	/// </summary>
	/// <remarks>
	/// Keyed on the control rather than on the binding expression, which is replaced without notification
	/// whenever the property is rebound.
	/// </remarks>
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

			Synchronize();
		}

		public void Dispose()
		{
			_subscription.Dispose();
			_expression = null;
			_source = null;
			_propertyName = null;
		}

		private void OnErrorsChanged()
		{
			if (NativeDispatcher.Main.HasThreadAccess)
			{
				Synchronize();
			}
			else
			{
				// A view model may raise ErrorsChanged from any thread; the property system is UI-thread bound.
				_control.DispatcherQueue.TryEnqueue(Synchronize);
			}
		}

		private void Synchronize()
		{
			if (_source?.Target is not INotifyDataErrorInfo source)
			{
				return;
			}

			_control.UpdateValidationErrors(source.GetErrors(_propertyName));
		}

		/// <summary>
		/// Subscribes without letting the source root the control: the event closure holds only a weak
		/// reference to this subscription, which the control keeps alive through its field, and
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
					target.OnErrorsChanged();
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
