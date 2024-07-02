#nullable enable

#if !NETFX_CORE
using System;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Input;
using Windows.ApplicationModel.Appointments;
using Windows.UI.Xaml;

namespace Uno.UI.DataBinding
{
	internal partial class BindingPath
	{
		private sealed class BindingItem : IBindingItem, IDisposable, IEquatable<BindingItem>
		{
			private ManagedWeakReference? _dataContextWeakStorage;
			private Flags _flags;

			private readonly SerialDisposable _propertyChanged = new SerialDisposable();
			private readonly DependencyPropertyValuePrecedences? _precedence;
			private ValueGetterHandler? _valueGetter;
			private ValueGetterHandler? _precedenceSpecificGetter;
			private ValueGetterHandler? _substituteValueGetter;
			private ValueSetterHandler? _valueSetter;
			private ValueSetterHandler? _localValueSetter;
			private ValueUnsetterHandler? _valueUnsetter;

			private Type? _dataContextType;

			[Flags]
			private enum Flags
			{
				None = 0,
				Disposed = 1 << 0,
				AllowPrivateMembers = 1 << 1,
				IsDependencyProperty = 1 << 2,
				IsDependencyPropertyValueSet = 1 << 3,
			}

			public BindingItem(BindingItem next, string property) :
				this(next, property, null, false)
			{
			}

			internal BindingItem(BindingItem? next, string property, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers)
			{
				Next = next;
				PropertyName = property;
				_precedence = precedence;
				AllowPrivateMembers = allowPrivateMembers;
			}

			public object? DataContext
			{
				get => _dataContextWeakStorage?.Target;
				set
				{
					if (!IsDisposed)
					{
						// Historically, Uno was processing property changes using INPC. Since the inclusion of DependencyObject
						// values changes are now filtered by DependencyProperty updates, making equality updates at this location
						// detrimental to the use of INPC events processing.
						// In case of an INPC, the bindings engine must reevaluate the path completely from the raising point, regardless
						// of the reference being changed.
						if (FeatureConfiguration.Binding.IgnoreINPCSameReferences && !DependencyObjectStore.AreDifferent(DataContext, value))
						{
							return;
						}

						var weakDataContext = WeakReferencePool.RentWeakReference(this, value);
						SetWeakDataContext(weakDataContext);
					}
				}
			}

			internal void SetWeakDataContext(ManagedWeakReference? weakDataContext)
			{
				var previousStorage = _dataContextWeakStorage;

				_dataContextWeakStorage = weakDataContext;
				OnDataContextChanged();

				// Return the reference to the pool after it's been released from the next BindingItem instances.
				// Failing to do so makes the reference change without the bindings knowing about it,
				// making the reference comparison always equal.
				Uno.UI.DataBinding.WeakReferencePool.ReturnWeakReference(this, previousStorage);
			}

			public BindingItem? Next { get; }
			public string PropertyName { get; }

			public BindingPath? Path { get; set; }

			public object? Value
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
			private void SetValue(object? value)
			{
				BuildValueSetter();
				SetSourceValue(_valueSetter!, value);
			}

			/// <summary>
			/// Sets the value using the <see cref="DependencyPropertyValuePrecedences.Local"/>
			/// </summary>
			/// <param name="value">The value to set</param>
			public void SetLocalValue(object value)
			{
				BuildLocalValueSetter();
				SetSourceValue(_localValueSetter!, value);
			}

			public Type? PropertyType
			{
				get
				{
					if (DataContext != null)
					{
						return BindingPropertyHelper.GetPropertyType(_dataContextType!, PropertyName, AllowPrivateMembers);
					}
					else
					{
						return null;
					}
				}
			}

			internal object? GetPrecedenceSpecificValue()
			{
				BuildPrecedenceSpecificValueGetter();

				return GetSourceValue(_precedenceSpecificGetter!);
			}

			internal object? GetSubstituteValue()
			{
				BuildSubstituteValueGetter();

				return GetSourceValue(_substituteValueGetter!);
			}

			private bool _isDataContextChanging;

			private void OnDataContextChanged()
			{
				if (DataContext != null)
				{
					ClearCachedGetters();
					if (_propertyChanged.Disposable != null)
					{
						try
						{
							_isDataContextChanging = true;
							_propertyChanged.Disposable = null;
						}
						finally
						{
							_isDataContextChanging = false;
						}
					}

					_propertyChanged.Disposable = SubscribeToPropertyChanged();

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

			private void OnPropertyChanged(object? previousValue, object? newValue, bool shouldRaiseValueChanged)
			{
				if (_isDataContextChanging && newValue == DependencyProperty.UnsetValue)
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

				if (shouldRaiseValueChanged
					// If IgnoreINPCSameReferences is true, we will RaiseValueChanged only if previousValue != newValue
					// If IgnoreINPCSameReferences is false, we are not going to compare previousValue and newValue (which is the correct behavior).
					// In Uno 6, we should remove the following line.
					&& (!FeatureConfiguration.Binding.IgnoreINPCSameReferences || previousValue != newValue)
					)
				{
					// We should call RaiseValueChanged even if oldValue == newValue.
					// It's the responsibility of the user to only raise PropertyChanged event when needed.
					// Not calling RaiseValueChanged when oldValue == newValue is a bug because a sub-property could
					// have changed, and it can have an effect when applying the binding, for converters for example.
					RaiseValueChanged(newValue);
				}
			}

			private void ClearCachedGetters()
			{
				var currentType = DataContext!.GetType();

				if (_dataContextType != currentType && _dataContextType != null)
				{
					IsDependencyPropertyValueSet = false;
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

			private void SetSourceValue(ValueSetterHandler setter, object? value)
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
						if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
						{
							this.Log().Error($"Failed to set the source value for [{PropertyName}]", exception);
						}
					}
				}
				else
				{
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("Setting [{0}] failed because the DataContext is null for. It may have already been collected, or explicitly set to null.", PropertyName);
					}
				}
			}

			private void BuildValueGetter()
			{
				if (_valueGetter == null && _dataContextType != null)
				{
					_valueGetter = BindingPropertyHelper.GetValueGetter(_dataContextType, PropertyName, _precedence, AllowPrivateMembers);
				}
			}

			private void BuildPrecedenceSpecificValueGetter()
			{
				if (_precedenceSpecificGetter == null && _dataContextType != null)
				{
					_precedenceSpecificGetter = BindingPropertyHelper.GetValueGetter(_dataContextType, PropertyName, _precedence, AllowPrivateMembers);
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

			private object? GetSourceValue()
			{
				BuildValueGetter();

				return GetSourceValue(_valueGetter!);
			}

			private object? GetSourceValue(ValueGetterHandler getter)
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
						if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
						{
							this.Log().Error($"Failed to get the source value for [{PropertyName}]", exception);
						}

						return DependencyProperty.UnsetValue;
					}
				}
				else
				{
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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
					_valueUnsetter!(dataContext);
				}
				else
				{
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						this.Log().DebugFormat("Unsetting [{0}] failed because the DataContext is null for. It may have already been collected, or explicitly set to null.", PropertyName);
					}
				}
			}

			private void RaiseValueChanged(object? newValue)
			{
				Path?.OnValueChanged(newValue);
			}

			/// <summary>
			/// Subscribes to property notifications for the current binding
			/// </summary>
			/// <param name="action">The action to execute when new values are raised</param>
			/// <returns>A disposable to be called when the subscription is disposed.</returns>
			private IDisposable SubscribeToPropertyChanged()
			{
				var disposables = new CompositeDisposable(_propertyChangedHandlers.Count + 1);
				if (SubscribeToNotifyCollectionChanged(this) is { } notifyCollectionChangedDisposable)
				{
					disposables.Add(notifyCollectionChangedDisposable);
				}

				for (var i = 0; i < _propertyChangedHandlers.Count; i++)
				{
					var handler = _propertyChangedHandlers[i];

					var valueHandler = new PropertyChangedValueHandler(this);

					var handlerDisposable = handler.Register(_dataContextWeakStorage!, PropertyName, valueHandler);

					if (handlerDisposable != null)
					{
						if (FeatureConfiguration.Binding.IgnoreINPCSameReferences)
						{
							// GetSourceValue calls into user code.
							// Avoid this if PreviousValue isn't going to be used at all.
							valueHandler.PreviousValue = GetSourceValue();
						}

						// We need to keep the reference to the updatePropertyHandler
						// in this disposable. The reference is attached to the source's
						// object lifetime, to the target (bound) object.
						//
						// All registrations made by _propertyChangedHandlers are
						// weak with regards to the delegates that are provided.
						disposables.Add(() =>
						{
							var previousValue = valueHandler.PreviousValue;

							valueHandler = null;
							handlerDisposable.Dispose();
							OnPropertyChanged(previousValue, DependencyProperty.UnsetValue, shouldRaiseValueChanged: false);
						});
					}
				}

				return disposables;
			}

			public void Dispose()
			{
				IsDisposed = true;
				_propertyChanged.Dispose();
			}

			private bool IsDisposed
			{
				get => (_flags & Flags.Disposed) != 0;
				set => SetFlag(value, Flags.Disposed);
			}

			private bool AllowPrivateMembers
			{
				get => (_flags & Flags.AllowPrivateMembers) != 0;
				set => SetFlag(value, Flags.AllowPrivateMembers);
			}

			private bool IsDependencyPropertyValueSet
			{
				get => (_flags & Flags.IsDependencyPropertyValueSet) != 0;
				set => SetFlag(value, Flags.IsDependencyPropertyValueSet);
			}

			internal bool IsDependencyProperty
			{
				get
				{
					if (!IsDependencyPropertyValueSet)
					{
						var isDP =
							_dataContextType is not null
							&& DependencyProperty.GetProperty(_dataContextType!, PropertyName) is not null;

						SetFlag(isDP, Flags.IsDependencyProperty);

						IsDependencyPropertyValueSet = true;

						return isDP;
					}
					else
					{
						return (_flags & Flags.IsDependencyProperty) != 0;
					}
				}
			}

			private void SetFlag(bool value, Flags flag)
			{
				if (!value)
				{
					_flags &= ~flag;
				}
				else
				{
					_flags |= flag;
				}
			}

			/// <summary>
			/// Property changed value handler, used to avoid creating a delegate for processing
			/// </summary>
			/// <remarks>
			/// This class is primarily used to avoid the costs associated with creating, storing and invoking delegates,
			/// particularly on WebAssembly as of .NET 6 where invoking a delegate requires a context switch from AOT
			/// to the interpreter.
			/// </remarks>
			private class PropertyChangedValueHandler : IPropertyChangedValueHandler, IWeakReferenceProvider
			{
				private readonly BindingItem _owner;
				private readonly ManagedWeakReference _self;

				public PropertyChangedValueHandler(BindingItem owner)
				{
					_owner = owner;
					_self = WeakReferencePool.RentSelfWeakReference(this);
				}

				public object? PreviousValue { get; set; }

				public ManagedWeakReference WeakReference
					=> _self;

				public void NewValue()
				{
					var newValue = _owner.GetSourceValue();

					_owner.OnPropertyChanged(PreviousValue, newValue, shouldRaiseValueChanged: true);

					PreviousValue = newValue;
				}

				public void NewValue(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
					=> NewValue();
			}

			/// <remarks>
			/// This is defined so that 2 BindingItems are equal if they refer to the same property on the same object
			/// even if they're a part of 2 different "chains".
			/// </remarks>
			public bool Equals(BindingItem? that)
			{
				return that != null
					&& this.PropertyType == that.PropertyType
					&& !DependencyObjectStore.AreDifferent(this.DataContext, that.DataContext)
					&& ComparePropertyName(this.PropertyName, that.PropertyName);

				// This is a naive comparison that most definitely doesn't match WinUI, but it should be good enough
				// for almost all cases.
				static bool ComparePropertyName(string name1, string name2)
				{
					if (name1.StartsWith('['))
					{
						// for indexers, we look for an identical match.
						return name1 == name2;
					}
					else
					{
						// e.g. "(Windows.UI.Xaml.Controls.Border.Background)" and "Background" should match.
						return name1.Replace(")", "").Replace("(", "").Split(':', '.')[^1] ==
							name2.Replace(")", "").Replace("(", "").Split(':', '.')[^1];
					}
				}
			}
		}
	}
}
#endif
