#if !NETFX_CORE
using Uno.UI.DataBinding;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Uno.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Uno.UI.DataBinding
{
	[DebuggerDisplay("Path={_path} DataContext={_dataContext}")]
	internal class BindingPath : IDisposable, IValueChangedListener
	{
		private static List<PropertyChangedRegistrationHandler> _propertyChangedHandlers = new List<PropertyChangedRegistrationHandler>();
		private readonly string _path;

		private BindingItem _chain;
		private BindingItem _value;
		private ManagedWeakReference _dataContextWeakStorage;
		private bool _disposed;

		/// <summary>
		/// Defines a delegate that will create a registration on the specified <paramref name="dataContext"/>> for the specified <paramref name="propertyName"/>.
		/// </summary>
		/// <param name="dataContext">The datacontext to use</param>
		/// <param name="propertyName">The property in the datacontext</param>
		/// <param name="onNewValue">The action to execute when a new value is raised</param>
		/// <returns>A disposable that will cleanup resources.</returns>
		public delegate IDisposable PropertyChangedRegistrationHandler(ManagedWeakReference dataContext, string propertyName, Action onNewValue);

		/// <summary>
		/// Provides the new values for the current binding.
		/// </summary>
		/// <remarks>
		/// This event is not a generic type for performance constraints on Mono's Full-AOT
		/// </remarks>
		public IValueChangedListener ValueChangedListener { get; set; }

		static BindingPath()
		{
			RegisterPropertyChangedRegistrationHandler(SubscribeToNotifyPropertyChanged);
		}

		/// <summary>
		/// Creates a BindingPath for the specified path
		/// </summary>
		/// <param name="path"></param>
		/// <param name="fallbackValue">Provides the fallback value to apply when the source is invalid.</param>
		public BindingPath(string path, object fallbackValue) :
			this(path, fallbackValue, null, false)
		{
		}

		/// <summary>
		/// Creates a BindingPath for the specified path using a specified DependencyProperty precedence.
		/// </summary>
		/// <param name="path">The path to the property</param>
		/// <param name="fallbackValue">Provides the fallback value to apply when the source is invalid.</param>
		/// <param name="precedence">The precedence value to manipulate if the path matches to a DependencyProperty.</param>
		/// <param name="allowPrivateMembers">Allows for the binding engine to include private properties in the lookup</param>
		internal BindingPath(string path, object fallbackValue, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers)
		{
			_path = (path ?? "").Trim();

			var items = _path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

			var parsedItems = ParseDependencyPropertyAccess(items);

			BindingItem bindingItem = null;

			foreach (var item in parsedItems.Reverse())
			{
				bindingItem = new BindingItem(bindingItem, item, fallbackValue, precedence, allowPrivateMembers);
				_chain = bindingItem;

				if (_value == null)
				{
					_value = bindingItem;
				}
			}

			if (_value != null)
			{
				_value.ValueChangedListener = this;
			}
		}

		/// <summary>
		/// Returns a chain of items composing the currently databound path.
		/// </summary>
		/// <returns>An enumerable of binding items</returns>
		/// <remarks>
		/// The DataContext and PropertyType of the descriptor may be null
		/// if the binding is incomplete (the DataContext may be null, or the path is invalid)
		/// </remarks>
		public IEnumerable<IBindingItem> GetPathItems()
		{
			return _chain.Flatten(i => i.Next);
		}

		private IEnumerable<string> ParseDependencyPropertyAccess(string[] items)
		{
			var output = new List<string>();

			for (int i = 0; i < items.Length; i++)
			{
				var item = items[i];

				if (item.Length != 0 && item[0] == '(')
				{
					var sb = new StringBuilder();
					sb.Append(item);

					while (!item.EndsWith(")"))
					{
						sb.Append("." + (item = items[++i]));
					}

					yield return sb.ToString().Trim(new[] { '(', ')' });
				}
				else
				{
					yield return item;
				}
			}
		}

		/// <summary>
		/// Registers a property changed registration handler.
		/// </summary>
		/// <param name="handler">The handled to be called when a property needs to be observed.</param>
		/// <remarks>This method exists to provide layer separation,
		/// when BindingPath is in the presentation layer, and DependencyProperty is in the (some) Views layer.
		/// </remarks>
		public static void RegisterPropertyChangedRegistrationHandler(PropertyChangedRegistrationHandler handler)
		{
			_propertyChangedHandlers.Add(handler);
		}

		public string Path
		{
			get
			{
				return _path;
			}
		}

		/// <summary>
		/// Provides the value of the <see cref="Path"/> using the
		/// current <see cref="DataContext"/> using the current precedence.
		/// </summary>
		public object Value
		{
			get
			{
				if (_value != null)
				{
					return _value.Value;
				}
				else
				{
					return DataContext;
				}
			}
			set
			{
				if (!_disposed
					&& _value != null
					&& DependencyObjectStore.AreDifferent(value, _value.GetPrecedenceSpecificValue())
				)
				{
					_value.Value = value;
				}
			}
		}

		/// <summary>
		/// Gets the value of the DependencyProperty with a
		/// precedence immediately below the one specified at the creation
		/// of the BindingPath.
		/// </summary>
		/// <returns>The lower precedence value</returns>
		public object GetSubstituteValue()
		{
			if (_value != null)
			{
				return _value.GetSubstituteValue();
			}
			else
			{
				return DataContext;
			}
		}

		/// <summary>
		/// Sets the value of the <see cref="DependencyPropertyValuePrecedences.Local"/>
		/// </summary>
		/// <param name="value">The value to set</param>
		internal void SetLocalValue(object value)
		{
			if (!_disposed)
			{
				_value?.SetLocalValue(value);
			}
		}

		/// <summary>
		/// Clears the value of the current precedence.
		/// </summary>
		/// <remarks>After this call, the value returned
		/// by <see cref="Value"/> will be of the next available
		///  precedence.</remarks>
		public void ClearValue()
		{
			if (!_disposed)
			{
				_value?.ClearValue();
			}
		}

		public Type ValueType => _value.PropertyType;

		internal object DataItem => _value.DataContext;

		public object DataContext
		{
			get => _dataContextWeakStorage?.Target;
			set => SetWeakDataContext(Uno.UI.DataBinding.WeakReferencePool.RentWeakReference(this, value));
		}

		internal void SetWeakDataContext(ManagedWeakReference weakDataContext)
		{
			if (!_disposed)
			{
				var previousStorage = _dataContextWeakStorage;

				_dataContextWeakStorage = weakDataContext;
				OnDataContextChanged();

				// Return the reference to the pool after it's been released from the underlying BindingItem instances.
				// Failing to do so makes the reference change without the bindings knowing about it,
				// making the reference comparison always equal.
				Uno.UI.DataBinding.WeakReferencePool.ReturnWeakReference(this, previousStorage);
			}
		}

		private void OnDataContextChanged()
		{
			if (_chain != null)
			{
				_chain.SetWeakDataContext(_dataContextWeakStorage);
			}
			else
			{
				// This is an empty path binding, raise the current value as changed.
				ValueChangedListener?.OnValueChanged(Value);
			}
		}

		void IValueChangedListener.OnValueChanged(object o)
		{
			ValueChangedListener?.OnValueChanged(o);
		}

		~BindingPath()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			_disposed = true;

			if (disposing)
			{
				_value.SafeDispose();
				_chain.SafeDispose();
			}
		}

		/// <summary>
		/// Subscibes for updates to the INotifyPropertyChanged interface.
		/// </summary>
		private static IDisposable SubscribeToNotifyPropertyChanged(ManagedWeakReference dataContextReference, string propertyName, Action newValueAction)
		{
			// Attach to the Notify property changed events
			var notify = dataContextReference.Target as INotifyPropertyChanged;

			if (notify != null)
			{
				if (propertyName.Length != 0 && propertyName[0] == '[')
				{
					propertyName = "Item" + propertyName;
				}

				var newValueActionWeak = Uno.UI.DataBinding.WeakReferencePool.RentWeakReference(null, newValueAction);

				PropertyChangedEventHandler handler = (s, args) =>
				{
					if (args.PropertyName == propertyName)
					{
						if (typeof(BindingPath).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
						{
							typeof(BindingPath).Log().Debug("Property changed for {0} on [{1}]".InvariantCultureFormat(propertyName, dataContextReference.Target?.GetType()));
						}

						(newValueActionWeak.Target as Action)?.Invoke();
					}
				};

				notify.PropertyChanged += handler;

				return Disposable.Create(() =>
				{
					// This weak reference ensure that the closure will not link
					// the caller and the callee, in the same way "newValueActionWeak"
					// does not link the callee to the caller.
					var that = dataContextReference.Target as INotifyPropertyChanged;

					if (that != null)
					{
						that.PropertyChanged -= handler;
					}

					Uno.UI.DataBinding.WeakReferencePool.ReturnWeakReference(null, newValueActionWeak);
				});
			}

			return null;
		}

		private sealed class BindingItem : IBindingItem, IDisposable
		{
			private delegate void PropertyChangedHandler(object previousValue, object newValue, bool shouldRaiseValueChanged);

			private ManagedWeakReference _dataContextWeakStorage;

			private readonly SerialDisposable _propertyChanged = new SerialDisposable();
			private bool _disposed;
			private readonly DependencyPropertyValuePrecedences? _precedence;
			private readonly object _fallbackValue;
			private readonly bool _allowPrivateMembers;
			private ValueGetterHandler _valueGetter;
			private ValueGetterHandler _precedenceSpecificGetter;
			private ValueGetterHandler _substituteValueGetter;
			private ValueSetterHandler _valueSetter;
			private ValueSetterHandler _localValueSetter;
			private ValueUnsetterHandler _valueUnsetter;

			private Type _dataContextType;

			public BindingItem(BindingItem next, string property, object fallbackValue) :
				this(next, property, fallbackValue, null, false)
			{
			}

			internal BindingItem(BindingItem next, string property, object fallbackValue, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers)
			{
				Next = next;
				PropertyName = property;
				_precedence = precedence;
				_fallbackValue = fallbackValue;
				_allowPrivateMembers = allowPrivateMembers;
			}

			public object DataContext
			{
				get => _dataContextWeakStorage?.Target;
				set
				{
					if (!_disposed && DependencyObjectStore.AreDifferent(DataContext, value))
					{
						var weakDataContext = WeakReferencePool.RentWeakReference(this, value);
						SetWeakDataContext(weakDataContext);
					}
				}
			}

			internal void SetWeakDataContext(ManagedWeakReference weakDataContext, bool transferReferenceOwnership = false)
			{
				var previousStorage = _dataContextWeakStorage;

				_dataContextWeakStorage = weakDataContext;
				OnDataContextChanged();

				// Return the reference to the pool after it's been released from the next BindingItem instances.
				// Failing to do so makes the reference change without the bindings knowing about it,
				// making the reference comparison always equal.
				Uno.UI.DataBinding.WeakReferencePool.ReturnWeakReference(this, previousStorage);
			}

			public BindingItem Next { get; }
			public string PropertyName { get; }

			public IValueChangedListener ValueChangedListener { get; set; }

			public object Value
			{
				get
				{
					return GetSourceValue();
				}
				set
				{
					SetValue(value);
				}
			}

			/// <summary>
			/// Sets the value using the <see cref="_precedence"/>
			/// </summary>
			/// <param name="value">The value to set</param>
			private void SetValue(object value)
			{
				BuildValueSetter();
				SetSourceValue(_valueSetter, value);
			}

			/// <summary>
			/// Sets the value using the <see cref="DependencyPropertyValuePrecedences.Local"/>
			/// </summary>
			/// <param name="value">The value to set</param>
			public void SetLocalValue(object value)
			{
				BuildLocalValueSetter();
				SetSourceValue(_localValueSetter, value);
			}

			public Type PropertyType
			{
				get
				{
					if (DataContext != null)
					{
						return BindingPropertyHelper.GetPropertyType(_dataContextType, PropertyName);
					}
					else
					{
						return null;
					}
				}
			}

			internal object GetPrecedenceSpecificValue()
			{
				BuildPrecedenceSpecificValueGetter();

				return GetSourceValue(_precedenceSpecificGetter);
			}

			internal object GetSubstituteValue()
			{
				BuildSubstituteValueGetter();

				return GetSourceValue(_substituteValueGetter);
			}

			private bool _isDataContextChanging;

			private void OnDataContextChanged()
			{
				if (DataContext != null)
				{
					ClearCachedGetters();
					if (_propertyChanged.Disposable != null)
					{
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/mono/mono/issues/13653
						try
#endif
						{
							_isDataContextChanging = true;
							_propertyChanged.Disposable = null;
						}
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/mono/mono/issues/13653
						finally
#endif
						{
							_isDataContextChanging = false;
						}
					}

					_propertyChanged.Disposable =
							SubscribeToPropertyChanged((previousValue, newValue, shouldRaiseValueChanged) =>
								{
									if (_isDataContextChanging && newValue is UnsetValue)
									{
										// We're in a "resubscribe" scenario when the DataContext is provided a new non-null value, so we don't need to
										// pass through the DependencyProperty.UnsetValue.
										// We simply discard this update.
										return;
									}

									if (Next != null)
									{
										Next.DataContext = newValue;
									}

									if (shouldRaiseValueChanged && previousValue != newValue)
									{
										RaiseValueChanged(newValue);
									}
								}
							);

					RaiseValueChanged(Value);

					if (Next != null)
					{
						Next.DataContext = Value;
					}
				}
				else
				{
					if (Next != null)
					{
						Next.DataContext = null;
					}
					RaiseValueChanged(null);

					_propertyChanged.Disposable = null;
				}
			}

			private void ClearCachedGetters()
			{
				var currentType = DataContext.GetType();

				if (_dataContextType != currentType && _dataContextType != null)
				{
					_valueGetter = null;
					_precedenceSpecificGetter = null;
					_substituteValueGetter = null;
					_localValueSetter = null;
					_valueSetter = null;
					_valueUnsetter = null;
				}

				_dataContextType = currentType;
			}

			private void BuildValueSetter()
			{
				if (_valueSetter == null && _dataContextType != null)
				{
					if (_precedence == null)
					{
						BuildLocalValueSetter();
						_valueSetter = _localValueSetter;
					}
					else
					{
						_valueSetter = BindingPropertyHelper.GetValueSetter(_dataContextType, PropertyName, convert: true, precedence: _precedence.Value);
					}
				}
			}

			private void BuildLocalValueSetter()
			{
				if (_localValueSetter == null && _dataContextType != null)
				{
					_localValueSetter = BindingPropertyHelper.GetValueSetter(_dataContextType, PropertyName, convert: true);
				}
			}

			private void SetSourceValue(ValueSetterHandler setter, object value)
			{
				// Capture the datacontext before the call to avoid a race condition with the GC.
				var dataContext = DataContext;

				if (dataContext != null)
				{
					try
					{
						setter(dataContext, value);
					}
					catch (Exception exception)
					{
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
						{
							this.Log().Error($"Failed to set the source value for [{PropertyName}]", exception);
						}
					}
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("Setting [{0}] failed because the DataContext is null for. It may have already been collected, or explicitly set to null.", PropertyName);
					}
				}
			}

			private void BuildValueGetter()
			{
				if (_valueGetter == null && _dataContextType != null)
				{
					_valueGetter = BindingPropertyHelper.GetValueGetter(_dataContextType, PropertyName, _precedence, _allowPrivateMembers);
				}
			}

			private void BuildPrecedenceSpecificValueGetter()
			{
				if (_precedenceSpecificGetter == null && _dataContextType != null)
				{
					_precedenceSpecificGetter = BindingPropertyHelper.GetValueGetter(_dataContextType, PropertyName, _precedence, _allowPrivateMembers);
				}
			}

			private void BuildSubstituteValueGetter()
			{
				if (_substituteValueGetter == null && _dataContextType != null)
				{
					_substituteValueGetter =
						BindingPropertyHelper.GetSubstituteValueGetter(_dataContextType, PropertyName, _precedence ?? DependencyPropertyValuePrecedences.Local);
				}
			}

			private object GetSourceValue()
			{
				BuildValueGetter();

				return GetSourceValue(_valueGetter);
			}

			private object GetSourceValue(ValueGetterHandler getter)
			{
				// Capture the datacontext before the call to avoid a race condition with the GC.
				var dataContext = DataContext;

				if (dataContext != null)
				{
					try
					{
						return getter(dataContext);
					}
					catch (Exception exception)
					{
						if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
						{
							this.Log().Error($"Failed to get the source value for [{PropertyName}]", exception);
						}

						return DependencyProperty.UnsetValue;
					}
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("Unable to get the source value for [{0}]", PropertyName);
					}

					return null;
				}
			}

			private void BuildValueUnsetter()
			{
				if (_valueUnsetter == null && _dataContextType != null)
				{
					_valueUnsetter = _precedence == null ?
						BindingPropertyHelper.GetValueUnsetter(_dataContextType, PropertyName) :
						BindingPropertyHelper.GetValueUnsetter(_dataContextType, PropertyName, precedence: _precedence.Value);
				}
			}

			internal void ClearValue()
			{
				BuildValueUnsetter();

				// Capture the datacontext before the call to avoid a race condition with the GC.
				var dataContext = DataContext;

				if (dataContext != null)
				{
					_valueUnsetter(dataContext);
				}
				else
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("Unsetting [{0}] failed because the DataContext is null for. It may have already been collected, or explicitly set to null.", PropertyName);
					}
				}
			}

			private void RaiseValueChanged(object newValue)
			{
				ValueChangedListener?.OnValueChanged(newValue);
			}

			/// <summary>
			/// Subscribes to property notifications for the current binding
			/// </summary>
			/// <param name="action">The action to execute when new values are raised</param>
			/// <returns>A disposable to be called when the subscription is disposed.</returns>
			private IDisposable SubscribeToPropertyChanged(PropertyChangedHandler action)
			{
				var disposables = new CompositeDisposable((_propertyChangedHandlers.Count * 3));
				foreach (var handler in _propertyChangedHandlers)
				{
					object previousValue = default;

					Action updateProperty = () =>
					{
						var newValue = GetSourceValue();

						action(previousValue, newValue, shouldRaiseValueChanged: true);

						previousValue = newValue;
					};

					Action disposeAction = () =>
					{
						action(previousValue, DependencyProperty.UnsetValue, shouldRaiseValueChanged: false);
					};

					var handlerDisposable = handler(_dataContextWeakStorage, PropertyName, updateProperty);

					if (handlerDisposable != null)
					{
						previousValue = GetSourceValue();

						// We need to keep the reference to the updatePropertyHandler
						// in this disposable. The reference is attached to the source's
						// object lifetime, to the target (bound) object.
						//
						// All registrations made by _propertyChangedHandlers are
						// weak with regards to the delegates that are provided.
						disposables.Add(() => updateProperty = null);
						disposables.Add(handlerDisposable);
						disposables.Add(disposeAction);
					}
				}

				return disposables;
			}

			public void Dispose()
			{
				_disposed = true;
				_propertyChanged.Dispose();
			}
		}
	}
}
#endif
