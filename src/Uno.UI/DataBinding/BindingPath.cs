#nullable enable

#if !NETFX_CORE
using Uno.UI.DataBinding;
using Uno.Extensions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.DataBinding
{
	[DebuggerDisplay("Path={_path} DataContext={_dataContext}")]
	internal class BindingPath : IDisposable, IValueChangedListener
	{
		private static List<IPropertyChangedRegistrationHandler> _propertyChangedHandlers = new List<IPropertyChangedRegistrationHandler>(2);
		private readonly string _path;

		private BindingItem? _chain;
		private BindingItem? _value;
		private ManagedWeakReference? _dataContextWeakStorage;
		private bool _disposed;

		/// <summary>
		/// Defines a interface that will allow for the creation of a registration on the specified dataContext
		/// for the specified propertyName.
		/// </summary>
		public interface IPropertyChangedRegistrationHandler
		{
			/// <summary>
			/// Registere a new <see cref="IPropertyChangedValueHandler"/> for the specified property
			/// </summary>
			/// <param name="dataContext">The datacontext to use</param>
			/// <param name="propertyName">The property in the datacontext</param>
			/// <param name="onNewValue">The action to execute when a new value is raised</param>
			/// <returns>A disposable that will cleanup resources.</returns>
			IDisposable? Register(ManagedWeakReference dataContext, string propertyName, IPropertyChangedValueHandler onNewValue);
		}

		/// <summary>
		/// PropertyChanged value handler.
		/// </summary>
		/// <remarks>
		/// This is an interface to avoid the use of delegates, and delegates type conversion as
		/// there are two available signatures. (<see cref="Action"/> and <see cref="DependencyPropertyChangedCallback"/>)
		/// </remarks>
		public interface IPropertyChangedValueHandler
		{
			/// <summary>
			/// Process a property changed using the <see cref="DependencyPropertyChangedCallback"/> signature.
			/// </summary>
			void NewValue(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args);

			/// <summary>
			/// Processa a property changed using <see cref="Action"/>-like signature (e.g. for <see cref="BindingItem"/>)
			/// </summary>
			void NewValue();
		}

		/// <summary>
		/// Provides the new values for the current binding.
		/// </summary>
		/// <remarks>
		/// This event is not a generic type for performance constraints on Mono's Full-AOT
		/// </remarks>
		public IValueChangedListener? ValueChangedListener { get; set; }

		static BindingPath()
		{
			RegisterPropertyChangedRegistrationHandler(new BindingPathPropertyChangedRegistrationHandler());
		}

		/// <summary>
		/// Creates a BindingPath for the specified path
		/// </summary>
		/// <param name="path"></param>
		/// <param name="fallbackValue">Provides the fallback value to apply when the source is invalid.</param>
		public BindingPath(string path, object? fallbackValue) :
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
		internal BindingPath(string path, object? fallbackValue, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers)
		{
			_path = path ?? "";

			Parse(_path, fallbackValue, precedence, allowPrivateMembers, ref _chain, ref _value);

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
			return _chain?.Flatten(i => i.Next!) ?? Array.Empty<BindingItem>();
		}

		/// <summary>
		/// Checks the property path for members which may be shared resources (<see cref="Brush"/>es and <see cref="Transform"/>s) and creates a
		/// copy of them if need be (ie if not already copied). Intended to be used prior to animating the targeted property.
		/// </summary>
		internal void CloneShareableObjectsInPath()
		{
			foreach (BindingItem item in GetPathItems())
			{
				if (item.PropertyType == typeof(Brush) || item.PropertyType == typeof(GeneralTransform))
				{
					if (item.Value is IShareableDependencyObject shareable && !shareable.IsClone && item.DataContext is DependencyObject owner)
					{
						var clone = shareable.Clone();

						item.Value = clone;
						break;
					}
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
		public static void RegisterPropertyChangedRegistrationHandler(IPropertyChangedRegistrationHandler handler)
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
		public object? Value
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
		/// Name of the leaf property in the path.
		/// </summary>
		internal string? LeafPropertyName => _value?.PropertyName;

		/// <summary>
		/// Gets the value of the DependencyProperty with a
		/// precedence immediately below the one specified at the creation
		/// of the BindingPath.
		/// </summary>
		/// <returns>The lower precedence value</returns>
		public object? GetSubstituteValue()
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

		public Type? ValueType => _value?.PropertyType;

		internal object? DataItem => _value?.DataContext;

		public object? DataContext
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
		/// Property changed registration handler for BindingPath.
		/// </summary>
		private class BindingPathPropertyChangedRegistrationHandler : IPropertyChangedRegistrationHandler
		{
			public IDisposable? Register(ManagedWeakReference dataContext, string propertyName, IPropertyChangedValueHandler onNewValue)
				=> SubscribeToNotifyPropertyChanged(dataContext, propertyName, onNewValue);
		}

		#region Miscs helpers
		/// <summary>
		/// Parse the given string path in parts and create the linked list of binding items in head and tail
		/// </summary>
		private static void Parse(
			string path,
			object? fallbackValue, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers,
			ref BindingItem? head, ref BindingItem? tail)
		{
			var propertyLength = 0;
			bool isInAttachedProperty = false, isInItemIndex = false;
			for (var i = path.Length - 1; i >= 0; i--)
			{
				var c = path[i];
				switch (c)
				{
					case ')':
						TryPrependItem(path, i + 1, propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
						isInAttachedProperty = true;
						propertyLength = 0;
						break;

					case '(' when isInAttachedProperty:
						TryPrependItem(path, i + 1, propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
						isInAttachedProperty = false;
						propertyLength = 0;
						break;

					case ']':
						TryPrependItem(path, i + 1, propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
						isInItemIndex = true;
						propertyLength = 1; // We include the brackets for itemIndex properties
						break;

					case '[' when isInItemIndex:
						// Note: We use 'start = i' and '++propertyLength' here for 'TryPrependItem' as we include the brackets for itemIndex properties
						TryPrependItem(path, i, ++propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
						isInItemIndex = false;
						propertyLength = 0;
						break;

					case '.' when !isInAttachedProperty:
						TryPrependItem(path, i + 1, propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
						propertyLength = 0;
						break;

					default:
						if (propertyLength > 0 || !char.IsWhiteSpace(c)) // i.e. TrimEnd
						{
							propertyLength++;
						}
						break;
				}
			}

			TryPrependItem(path, 0, propertyLength, fallbackValue, precedence, allowPrivateMembers, ref head, ref tail);
		}

		/// <summary>
		/// Prepends an item in the binding linked list if the string defined between by start and length is valid.
		/// </summary>
		private static void TryPrependItem(
			string path, int start, int length,
			object? fallbackValue, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers,
			ref BindingItem? head, ref BindingItem? tail)
		{
			if (length <= 0)
			{
				return;
			}

			// Trim start (trim end is achieved in the main Parse loop)
			var c = path[start];
			while (char.IsWhiteSpace(c) && length > 0)
			{
				start++;
				length--;
				c = path[start];
			}

			if (length <= 0)
			{
				return;
			}

			var itemPath = path.Substring(start, length);
			var item = new BindingItem(head, itemPath, fallbackValue, precedence, allowPrivateMembers);

			head = item;
			tail ??= item;
		}

		/// <summary>
		/// Subscribes for updates to the INotifyPropertyChanged interface.
		/// </summary>
		private static IDisposable? SubscribeToNotifyPropertyChanged(ManagedWeakReference dataContextReference, string propertyName, IPropertyChangedValueHandler propertyChangedValueHandler)
		{
			// Attach to the Notify property changed events
			var notify = dataContextReference.Target as System.ComponentModel.INotifyPropertyChanged;

			if (notify != null)
			{
				if (propertyName.Length != 0 && propertyName[0] == '[')
				{
					propertyName = "Item" + propertyName;
				}

				var newValueActionWeak = Uno.UI.DataBinding.WeakReferencePool.RentWeakReference(null, propertyChangedValueHandler);

				System.ComponentModel.PropertyChangedEventHandler handler = (s, args) =>
				{
					if (args.PropertyName == propertyName || string.IsNullOrEmpty(args.PropertyName))
					{
						if (typeof(BindingPath).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
						{
							typeof(BindingPath).Log().Debug($"Property changed for {propertyName} on [{dataContextReference.Target?.GetType()}]");
						}

						if (!newValueActionWeak.IsDisposed
						&& newValueActionWeak.Target is IPropertyChangedValueHandler handler)
						{
							handler.NewValue();
						}
					}
				};

				notify.PropertyChanged += handler;

				return Disposable.Create(() =>
				{
					// This weak reference ensure that the closure will not link
					// the caller and the callee, in the same way "newValueActionWeak"
					// does not link the callee to the caller.
					var that = dataContextReference.Target as System.ComponentModel.INotifyPropertyChanged;

					if (that != null)
					{
						that.PropertyChanged -= handler;
					}

					Uno.UI.DataBinding.WeakReferencePool.ReturnWeakReference(null, newValueActionWeak);
				});
			}

			return null;
		}
		#endregion

		private sealed class BindingItem : IBindingItem, IDisposable
		{
			private delegate void PropertyChangedHandler(object? previousValue, object? newValue, bool shouldRaiseValueChanged);

			private ManagedWeakReference? _dataContextWeakStorage;

			private readonly SerialDisposable _propertyChanged = new SerialDisposable();
			private bool _disposed;
			private readonly DependencyPropertyValuePrecedences? _precedence;
			private readonly object? _fallbackValue;
			private readonly bool _allowPrivateMembers;
			private ValueGetterHandler? _valueGetter;
			private ValueGetterHandler? _precedenceSpecificGetter;
			private ValueGetterHandler? _substituteValueGetter;
			private ValueSetterHandler? _valueSetter;
			private ValueSetterHandler? _localValueSetter;
			private ValueUnsetterHandler? _valueUnsetter;

			private Type? _dataContextType;

			public BindingItem(BindingItem next, string property, object fallbackValue) :
				this(next, property, fallbackValue, null, false)
			{
			}

			internal BindingItem(BindingItem? next, string property, object? fallbackValue, DependencyPropertyValuePrecedences? precedence, bool allowPrivateMembers)
			{
				Next = next;
				PropertyName = property;
				_precedence = precedence;
				_fallbackValue = fallbackValue;
				_allowPrivateMembers = allowPrivateMembers;
			}

			public object? DataContext
			{
				get => _dataContextWeakStorage?.Target;
				set
				{
					if (!_disposed)
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

			internal void SetWeakDataContext(ManagedWeakReference? weakDataContext, bool transferReferenceOwnership = false)
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

			public IValueChangedListener? ValueChangedListener { get; set; }

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
						return BindingPropertyHelper.GetPropertyType(_dataContextType!, PropertyName, _allowPrivateMembers);
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
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
						try
#endif
						{
							_isDataContextChanging = true;
							_propertyChanged.Disposable = null;
						}
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
						finally
#endif
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

			private void ClearCachedGetters()
			{
				var currentType = DataContext!.GetType();

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
				ValueChangedListener?.OnValueChanged(newValue);
			}

			/// <summary>
			/// Subscribes to property notifications for the current binding
			/// </summary>
			/// <param name="action">The action to execute when new values are raised</param>
			/// <returns>A disposable to be called when the subscription is disposed.</returns>
			private IDisposable SubscribeToPropertyChanged()
			{
				var disposables = new CompositeDisposable(_propertyChangedHandlers.Count);

				for (var i = 0; i < _propertyChangedHandlers.Count; i++)
				{
					var handler = _propertyChangedHandlers[i];

					var valueHandler = new PropertyChangedValueHandler(this);

					var handlerDisposable = handler.Register(_dataContextWeakStorage!, PropertyName, valueHandler);

					if (handlerDisposable != null)
					{
						valueHandler.PreviousValue = GetSourceValue();

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
				_disposed = true;
				_propertyChanged.Dispose();
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
		}
	}
}
#endif
