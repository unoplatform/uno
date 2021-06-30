#nullable enable

using System;
using Uno.UI.DataBinding;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.Logging;
using Uno.Diagnostics.Eventing;
using Uno.Disposables;
using System.Linq;
using System.Threading;
using Uno.Collections;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Windows.UI.Xaml.Data;
using Uno.UI;
using System.Collections;

#if XAMARIN_ANDROID
using View = Android.Views.View;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
#endif

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Defines a delegate to be called when a property value changes.
	/// </summary>
	/// <param name="property">The property being changed</param>
	/// <param name="args">The arguments for the changes of the property</param>
	public delegate void ExplicitPropertyChangedCallback(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args);

	/// <summary>
	/// Defines a delegate to be called when the parent of a <see cref="DependencyObject"/> changes.
	/// </summary>
	/// <param name="instance">The DependencyObject instance being updated</param>
	/// <param name="key">An optional key passed as a parameter to <see cref="DependencyObject.RegisterParentChangedCallback(object, ParentChangedCallback)"/>.</param>
	/// <param name="args">The arguments of the change</param>
	internal delegate void ParentChangedCallback(object instance, object? key, DependencyObjectParentChangedEventArgs args);

	/// <summary>
	/// Defines a Dependency Object
	/// </summary>
	public partial class DependencyObjectStore : IDisposable
	{
		public static class TraceProvider
		{
			public static readonly Guid Id = new Guid(0x430FC851, 0xE917, 0x4587, 0xAF, 0x7B, 0x5A, 0x1C, 0xE5, 0xA1, 0x94, 0x1D);
			public const int GetValue = 1;
			public const int SetValueStart = 2;
			public const int SetValueStop = 3;
			public const int CreationTask = 4;
			public const int DataContextChangedStart = 5;
			public const int DataContextChangedStop = 6;
		}

		private static readonly IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		/// <summary>
		/// A global static counter that is used to uniquely identify objects.
		/// This is primarily used during trace profiling.
		/// </summary>
		private static long _objectIdCounter;

		private bool _isDisposed;

		private readonly DependencyPropertyDetailsCollection _properties;
		private ResourceBindingCollection? _resourceBindings;

		private DependencyProperty _parentTemplatedParentProperty = UIElement.TemplatedParentProperty;
		private DependencyProperty _parentDataContextProperty = UIElement.DataContextProperty;

		private ImmutableList<ExplicitPropertyChangedCallback> _genericCallbacks = ImmutableList<ExplicitPropertyChangedCallback>.Empty;
		private ImmutableList<DependencyObjectStore> _childrenStores = ImmutableList<DependencyObjectStore>.Empty;
		private ImmutableList<ParentChangedCallback> _parentChangedCallbacks = ImmutableList<ParentChangedCallback>.Empty;

		private readonly ManagedWeakReference _originalObjectRef;
		private DependencyObject? _hardOriginalObjectRef;

		/// <summary>
		/// This field is used to pass a reference to itself in the case
		/// of DependencyProperty changed registrations. This avoids creating many
		/// weak references to the same object.
		/// </summary>
		private ManagedWeakReference? _thisWeakRef;

		private readonly Type _originalObjectType;
		private readonly SerialDisposable _inheritedProperties = new SerialDisposable();
		private ManagedWeakReference? _parentRef;
		private object? _hardParentRef;
		private readonly Dictionary<DependencyProperty, ManagedWeakReference> _inheritedForwardedProperties = new Dictionary<DependencyProperty, ManagedWeakReference>(DependencyPropertyComparer.Default);
		private Stack<DependencyPropertyValuePrecedences?>? _overriddenPrecedences;

		private static long _propertyChangedToken = 0;
		private readonly Dictionary<long, IDisposable> _propertyChangedTokens = new Dictionary<long, IDisposable>();

		private bool _registeringInheritedProperties;
		private bool _unregisteringInheritedProperties;
		/// <summary>
		/// An ancestor store is unregistering inherited properties.
		/// </summary>
		private bool _parentUnregisteringInheritedProperties;
		/// <summary>
		/// Is a theme-bound value currently being set?
		/// </summary>
		private bool _isSettingThemeBinding;
		/// <summary>
		/// The theme last to apply theme bindings on this object and its children.
		/// </summary>
		private SpecializedResourceDictionary.ResourceKey? _themeLastUsed;

		private static readonly bool _validatePropertyOwner = Debugger.IsAttached;

		/// <summary>
		/// Provides the parent Dependency Object of this dependency object
		/// </summary>
		/// <remarks>
		/// This property is an <see cref="object"/> as the parent of a <see cref="DependencyObject"/> may
		/// not always be another <see cref="DependencyObject"/>, particularly in the case of the root element.
		/// </remarks>
		public object? Parent
		{
			get => _hardParentRef ?? _parentRef?.Target;
			set
			{
				if (
					!ReferenceEquals(_parentRef?.Target, value)
					|| (_parentRef != null && value is null)
				)
				{
					DisableHardReferences();

					var previousParent = _parentRef?.Target;

					if (_parentRef != null)
					{
						WeakReferencePool.ReturnWeakReference(this, _parentRef);
					}

					_parentRef = null;

					if (value != null)
					{
						_parentRef = WeakReferencePool.RentWeakReference(this, value);
					}

					_inheritedProperties.Disposable = null;

					if (value is IDependencyObjectStoreProvider parentProvider)
					{
						TryRegisterInheritedProperties(parentProvider);
					}

					OnParentChanged(previousParent, value);
				}
			}
		}

		static DependencyObjectStore()
		{
			InitializeStaticBinder();
		}

		/// <summary>
		/// Creates a delegated dependency object instance for the specified <paramref name="originalObject"/>
		/// </summary>
		/// <param name="originalObject"></param>
		public DependencyObjectStore(object originalObject, DependencyProperty dataContextProperty, DependencyProperty templatedParentProperty)
		{
			ObjectId = Interlocked.Increment(ref _objectIdCounter);

			_originalObjectRef = WeakReferencePool.RentWeakReference(this, originalObject);
			_originalObjectType = originalObject is AttachedDependencyObject a ? a.Owner.GetType() : originalObject.GetType();

			_properties = new DependencyPropertyDetailsCollection(_originalObjectType, _originalObjectRef, dataContextProperty, templatedParentProperty);

			_dataContextProperty = dataContextProperty;
			_templatedParentProperty = templatedParentProperty;

			if (_trace.IsEnabled)
			{
				_trace.WriteEvent(
					DependencyObjectStore.TraceProvider.CreationTask,
					new object[] { ObjectId, _originalObjectType.Name }
				);
			}
		}

		~DependencyObjectStore()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		private void Dispose(bool disposing)
		{
			if (disposing)
			{
				GC.SuppressFinalize(this);
			}

			BinderDispose();
		}

		/// <summary>
		/// Determines if the dependency object automatically registers for inherited
		/// properties such as <see cref="DataContextProperty"/> or <see cref="TemplatedParentProperty"/>.
		/// </summary>
		/// <remarks>
		/// This is used to avoid propagating the DataContext and TemplatedParent properties
		/// for types that commonly do not expose inherited propertyes, such as visual states.
		/// </remarks>
		public bool IsAutoPropertyInheritanceEnabled { get; set; } = true;

		/// <summary>
		/// Gets a unique identifier for the current DependencyObject.
		/// </summary>
		internal long ObjectId { get; }

		/// <summary>
		/// Returns the current effective value of a dependency property from a DependencyObject.
		/// </summary>
		/// <param name="property">The <see cref="DependencyProperty" /> identifier of the property for which to retrieve the value. </param>
		/// <returns>Returns the current effective value.</returns>
		public object? GetValue(DependencyProperty property)
		{
			return GetValue(property: property, propertyDetails: null, precedence: null, isPrecedenceSpecific: false);
		}

		/// <summary>
		/// Returns the local value of a dependency property, if a local value is set.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <returns></returns>
		public object? ReadLocalValue(DependencyProperty property)
		{
			return GetValue(property, precedence: DependencyPropertyValuePrecedences.Local, isPrecedenceSpecific: true);
		}

		/// <summary>
		/// Returns the local value of a dependency property, if a local value is set.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <returns></returns>
		public object? GetAnimationBaseValue(DependencyProperty property)
		{
			return GetValue(property, precedence: DependencyPropertyValuePrecedences.Local);
		}

		internal object? GetValue(DependencyProperty property, DependencyPropertyValuePrecedences? precedence = null, bool isPrecedenceSpecific = false)
		{
			return GetValue(property, null, precedence, isPrecedenceSpecific);
		}

		internal object? GetValue(DependencyProperty property, DependencyPropertyDetails? propertyDetails, DependencyPropertyValuePrecedences? precedence = null, bool isPrecedenceSpecific = false)
		{
			WritePropertyEventTrace(TraceProvider.GetValue, property, precedence);

			ValidatePropertyOwner(property);

			propertyDetails ??= _properties.GetPropertyDetails(property);

			return GetValue(propertyDetails, precedence, isPrecedenceSpecific);
		}

		private object? GetValue(DependencyPropertyDetails propertyDetails, DependencyPropertyValuePrecedences? precedence = null, bool isPrecedenceSpecific = false)
		{
			if (propertyDetails == _properties.DataContextPropertyDetails || propertyDetails == _properties.TemplatedParentPropertyDetails)
			{
				TryRegisterInheritedProperties(force: true);
			}

			if (precedence == null)
			{
				return propertyDetails.GetValue();
			}

			if (isPrecedenceSpecific)
			{
				return propertyDetails.GetValue(precedence.Value);
			}

			var highestPriority = GetCurrentHighestValuePrecedence(propertyDetails);

			return propertyDetails.GetValue((DependencyPropertyValuePrecedences)Math.Max((int)highestPriority, (int)precedence.Value));
		}

		/// <summary>
		/// Determines the current highest dependency property value precedence
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <returns></returns>
		internal DependencyPropertyValuePrecedences GetCurrentHighestValuePrecedence(DependencyPropertyDetails propertyDetails)
		{
			return propertyDetails.CurrentHighestValuePrecedence;
		}

		/// <summary>
		/// Determines the current highest dependency property value precedence
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <returns></returns>
		internal DependencyPropertyValuePrecedences GetCurrentHighestValuePrecedence(DependencyProperty property)
		{
			return _properties.GetPropertyDetails(property).CurrentHighestValuePrecedence;
		}

		/// <summary>
		/// Creates a SetValue precedence scoped override. All calls to SetValue
		/// on the specified instance will be set to the specified precedence.
		/// </summary>
		/// <param name="precedence">The precedence to set</param>
		/// <returns>A disposable to dispose to cancel the override.</returns>
		internal IDisposable? OverrideLocalPrecedence(DependencyPropertyValuePrecedences? precedence)
		{
			_overriddenPrecedences ??= new Stack<DependencyPropertyValuePrecedences?>(2);
			if (_overriddenPrecedences.Count > 0 && _overriddenPrecedences.Peek() == precedence)
			{
				return null; // this precedence is already set, no need to set a new one
			}

			_overriddenPrecedences.Push(precedence);

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"OverrideLocalPrecedence({precedence}) - stack is {string.Join(", ", _overriddenPrecedences)}");
			}

			return Disposable.Create(() =>
			{
				var popped = _overriddenPrecedences.Pop();
				if (popped != precedence)
				{
					throw new InvalidOperationException($"Error while unstacking precedence. Should be {precedence}, got {popped}.");
				}

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					var newPrecedence = _overriddenPrecedences.Count == 0 ? "<none>" : _overriddenPrecedences.Peek().ToString();
					this.Log().Debug($"OverrideLocalPrecedence({precedence}).Dispose() ==> new overriden precedence is {newPrecedence})");
				}
			});
		}

		private static readonly List<DependencyPropertyPath> _propagationBypass =
			new List<DependencyPropertyPath>();

		private static readonly Dictionary<DependencyPropertyPath, object?> _propagationBypassed =
			new Dictionary<DependencyPropertyPath, object?>(DependencyPropertyPath.Comparer.Default);

		internal static IDisposable? BypassPropagation(DependencyObject instance, DependencyProperty property)
		{
			var obj = instance;

			if (obj == null)
			{
				return null;
			}

			var path = new DependencyPropertyPath(obj, property);

			if (_propagationBypass.Contains(path, DependencyPropertyPath.Comparer.Default))
			{
				// Keep the current propagation bypass, which affects application of animated values
				return null;
			}
			else
			{
				_propagationBypass.Add(path);

				return Disposable.Create(() => _propagationBypass.Remove(path));
			}
		}

		/// <summary>
		/// Sets the local value of a dependency property on a <see cref="DependencyObject" />.
		/// </summary>
		/// <param name="property">The identifier of the <see cref="DependencyProperty"/> to set.</param>
		/// <param name="value">The new <see cref="DependencyPropertyValuePrecedences.Local"/> value.</param>
		public void SetValue(DependencyProperty property, object value)
		{
			SetValue(property, value, DependencyPropertyValuePrecedences.Local);
		}

		/// <summary>
		/// Clears the value for the specified dependency property on the specified instance.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <param name="precedence">The value precedence to assign</param>
		public void ClearValue(DependencyProperty property)
		{
			SetValue(property, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Local);
		}

		/// <summary>
		/// Clears the value for the specified dependency property on the specified instance.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <param name="precedence">The value precedence to assign</param>
		internal void ClearValue(DependencyProperty property, DependencyPropertyValuePrecedences precedence)
		{
			SetValue(property, DependencyProperty.UnsetValue, precedence);
		}

		internal void SetValue(DependencyProperty property, object? value, DependencyPropertyValuePrecedences precedence, DependencyPropertyDetails? propertyDetails = null, bool isThemeBinding = false)
		{
			if (_trace.IsEnabled)
			{
				using (WritePropertyEventTrace(TraceProvider.SetValueStart, TraceProvider.SetValueStop, property, precedence))
				{
					InnerSetValue(property, value, precedence, propertyDetails, isThemeBinding);
				}
			}
			else
			{
				InnerSetValue(property, value, precedence, propertyDetails, isThemeBinding);
			}

		}

		private void InnerSetValue(DependencyProperty property, object? value, DependencyPropertyValuePrecedences precedence, DependencyPropertyDetails? propertyDetails, bool isThemeBinding)
		{
			if (precedence == DependencyPropertyValuePrecedences.Coercion)
			{
				throw new ArgumentException("SetValue must not be called with precedence DependencyPropertyValuePrecedences.Coercion, as it expects a non-coerced value to function properly.");
			}

			var actualInstanceAlias = ActualInstance;

			if (actualInstanceAlias != null)
			{
				var overrideDisposable = ApplyPrecedenceOverride(ref precedence);

#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
				try
#endif
				{
					if ((value is UnsetValue) && precedence == DependencyPropertyValuePrecedences.DefaultValue)
					{
						throw new InvalidOperationException("The default value must be a valid value");
					}

					ValidatePropertyOwner(property);

					// Resolve the stack once for the instance, for performance.
					propertyDetails ??= _properties.GetPropertyDetails(property);

					var previousValue = GetValue(propertyDetails);
					var previousPrecedence = GetCurrentHighestValuePrecedence(propertyDetails);

					// Set even if they are different to make sure the value is now set on the right precedence
					SetValueInternal(value, precedence, propertyDetails);

					if (!isThemeBinding && !_isSettingThemeBinding)
					{
						// If a non-theme value is being set, clear any theme binding so it's not overwritten if the theme changes.
						_resourceBindings?.ClearBinding(property, precedence);
					}

					ApplyCoercion(actualInstanceAlias, propertyDetails, previousValue, value);

					// Value may or may not have changed based on the precedence
					var newValue = GetValue(propertyDetails);
					var newPrecedence = GetCurrentHighestValuePrecedence(propertyDetails);

					if (property == _dataContextProperty)
					{
						OnDataContextChanged(value, newValue, precedence);
					}

					TryUpdateInheritedAttachedProperty(property, propertyDetails);

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						var name = (_originalObjectRef.Target as IFrameworkElement)?.Name ?? _originalObjectRef.Target?.GetType().Name;
						var hashCode = _originalObjectRef.Target?.GetHashCode();

						this.Log().Debug(
							$"SetValue on [{name}/{hashCode:X8}] for [{property.Name}] to [{newValue}] (req:{value} reqp:{precedence} p:{previousValue} pp:{previousPrecedence} np:{newPrecedence})"
						);
					}

					RaiseCallbacks(actualInstanceAlias, propertyDetails, previousValue, previousPrecedence, newValue, newPrecedence);
				}
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
				finally
#endif
				{
					overrideDisposable?.Dispose();
				}
			}
			else
			{
				// The store has lost its current instance, renove it from its parent.
				Parent = null;
			}
		}

		private void TryUpdateInheritedAttachedProperty(DependencyProperty property, DependencyPropertyDetails propertyDetails)
		{
			if (
				property.IsAttached
				&& propertyDetails.Metadata is FrameworkPropertyMetadata fm
				&& fm.Options.HasInherits())
			{
				// Add inheritable attached properties to the inherited forwarded
				// properties, so they can be automatically propagated when a child
				// store is late added.
				_inheritedForwardedProperties[property] = _originalObjectRef;
			}
		}

		private void ApplyCoercion(DependencyObject actualInstanceAlias, DependencyPropertyDetails propertyDetails, object? previousValue, object? baseValue)
		{
			if (baseValue is UnsetValue)
			{
				// Removing any previously applied coercion
				SetValueInternal(DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Coercion, propertyDetails);

				// CoerceValueCallback shouldn't be called when unsetting the value.
				return;
			}

			var coerceValueCallback = propertyDetails.Metadata.CoerceValueCallback;
			if (coerceValueCallback == null)
			{
				// No coercion to remove or to apply.
				return;
			}

			if (!propertyDetails.Metadata.CoerceWhenUnchanged && Equals(previousValue, baseValue))
			{
				// Value hasn't changed, don't coerce.
				return;
			}

			var coercedValue = coerceValueCallback(actualInstanceAlias, baseValue);
			if (coercedValue is UnsetValue)
			{
				// The property system will treat any CoerceValueCallback that returns the value UnsetValue as a special case.
				// This special case means that the property change that resulted in the CoerceValueCallback being called
				// should be rejected by the property system, and that the property system should instead report whatever
				// previous value the property had.
				// Source: https://msdn.microsoft.com/en-us/library/ms745795%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
				SetValueInternal(previousValue, DependencyPropertyValuePrecedences.Coercion, propertyDetails);
			}
			else if (!Equals(coercedValue, baseValue))
			{
				// The base value and the coerced value are different, which means that coercion must be applied.
				// Set value using DependencyPropertyValuePrecedences.Coercion, which has the highest precedence.
				SetValueInternal(coercedValue, DependencyPropertyValuePrecedences.Coercion, propertyDetails);
			}
			else // Equals(coercedValue, baseValue)
			{
				// The base value and the coerced value are the same. Remove any existing coercion.
				SetValueInternal(DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Coercion, propertyDetails);
			}
		}

		internal void CoerceValue(DependencyProperty property)
		{
			// Trigger the coercion mechanism of SetValue, by re-applying the base value (non-coerced value).
			var (baseValue, basePrecedence) = GetValueUnderPrecedence(property, DependencyPropertyValuePrecedences.Coercion);
			SetValue(property, baseValue, basePrecedence);
		}

		private void WritePropertyEventTrace(int eventId, DependencyProperty property, DependencyPropertyValuePrecedences? precedence)
		{
			if (_trace.IsEnabled)
			{
				_trace.WriteEvent(eventId, new object[] { ObjectId, property.OwnerType.Name, property.Name, precedence?.ToString() ?? "Local" });
			}
		}

		private IDisposable? WritePropertyEventTrace(int startEventId, int stopEventId, DependencyProperty property, DependencyPropertyValuePrecedences precedence)
		{
			if (_trace.IsEnabled)
			{
				return _trace.WriteEventActivity(
					startEventId,
					stopEventId,
					new object[] { ObjectId, property.OwnerType.Name, property.Name, precedence.ToString() }
				);
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Applies the precedence override that has been set through <seealso cref="OverrideLocalPrecedence(object, DependencyPropertyValuePrecedences)" />
		/// Used to ambiently change the precedence specified when using standard DependencyProperty accessors, particularly when applying styles.
		/// </summary>
		/// <param name="precedence"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private IDisposable? ApplyPrecedenceOverride(ref DependencyPropertyValuePrecedences precedence)
		{
			var currentlyOverriddenPrecedence =
				_overriddenPrecedences?.Count > 0
					? _overriddenPrecedences.Peek()
					: default;

			if (currentlyOverriddenPrecedence is { } current)
			{
				precedence = current;

				// The ambient precedence is also set to "Local" for affected properties
				// (that means if properties are set in any callback, this override won't apply)
				return OverrideLocalPrecedence(null);
			}

			return null;
		}

		/// <summary>
		/// Validates that the DependencyProperty type is
		/// </summary>
		/// <param name="property">A dependency property</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ValidatePropertyOwner(DependencyProperty property)
		{
			if (_validatePropertyOwner)
			{
				var isFrameworkElement = _originalObjectType.Is(typeof(FrameworkElement));
				var isMixinFrameworkElement = _originalObjectRef.Target is IFrameworkElement && !isFrameworkElement;

				if (
					!_originalObjectType.Is(property.OwnerType)
					&& !property.IsAttached

					// Don't fail validation for properties that are located on non-FrameworkElement types
					// e.g. ScrollContentPresenter, for which using the Name property should not fail.
					&& !isMixinFrameworkElement
				)
				{
					throw new InvalidOperationException(
						$"The Dependency Property [{property.Name}] is owned by [{property.OwnerType}] and cannot be used on [{_originalObjectType}]"
					);
				}
			}
		}

		public long RegisterPropertyChangedCallback(DependencyProperty property, DependencyPropertyChangedCallback callback)
		{
			_propertyChangedToken = Interlocked.Increment(ref _propertyChangedToken);

			var registration = RegisterPropertyChangedCallback(property, (PropertyChangedCallback)((s, e) => callback((DependencyObject)s, property)));

			_propertyChangedTokens.Add(_propertyChangedToken, registration);

			return _propertyChangedToken;
		}

		public void UnregisterPropertyChangedCallback(DependencyProperty property, long token)
		{
			if (_propertyChangedTokens.TryGetValue(token, out var registration))
			{
				registration.Dispose();

				_propertyChangedTokens.Remove(token);
			}
		}

		internal IDisposable RegisterPropertyChangedCallback(DependencyProperty property, PropertyChangedCallback callback, DependencyPropertyDetails? propertyDetails = null)
		{
			var weakDelegate = CreateWeakDelegate(callback);

			propertyDetails ??= _properties.GetPropertyDetails(property);

			var cookie = propertyDetails.CallbackManager.RegisterCallback(weakDelegate.callback);

			// Capture the weak reference to this instance.
			var instanceRef = ThisWeakReference;

			return new DispatcherConditionalDisposable(
				callback.Target,
				instanceRef.CloneWeakReference(),
				() =>
				{
					// This weak reference ensure that the closure will not link
					// the caller and the callee, in the same way "newValueActionWeak"
					// does not link the callee to the caller.
					var that = instanceRef.Target as DependencyObjectStore;

					if (that != null)
					{
						cookie.Dispose();
						weakDelegate.release.Dispose();

						// Force a closure on the callback, to make its lifetime as long
						// as the subscription being held by the callee.
						callback = null!;
					}
				});
		}

		internal IDisposable RegisterPropertyChangedCallback(ExplicitPropertyChangedCallback handler)
		{
			var weakDelegate = CreateWeakDelegate(handler);

			// Delegates integrate a null check when adding new delegates.
			_genericCallbacks = _genericCallbacks.Add(weakDelegate.callback);

			// This weak reference ensure that the closure will not link
			// the caller and the callee, in the same way "newValueActionWeak"
			// does not link the callee to the caller.
			var instanceRef = ThisWeakReference;

			return new DispatcherConditionalDisposable(
				handler.Target,
				instanceRef.CloneWeakReference(),
				() =>
				{
					// This weak reference ensure that the closure will not link
					// the caller and the callee, in the same way "newValueActionWeak"
					// does not link the callee to the caller.
					var that = instanceRef.Target as DependencyObjectStore;

					if (that != null)
					{
						// Delegates integrate a null check when removing new delegates.
						that._genericCallbacks = that._genericCallbacks.Remove(weakDelegate.callback);
					}

					weakDelegate.release.Dispose();

					// Force a closure on the callback, to make its lifetime as long
					// as the subscription being held by the callee.
					handler = null!;
				}
			);
		}

		/// <summary>
		/// Registers an strong-referenced explicit DependencyProperty changed handler to be notified of any property change.
		/// </summary>
		/// <remarks>
		/// This method is meant to be used only for a DependencyObject to
		/// itself, to match the behavior of generic WinUI's OnPropertyChanged virtual method.
		/// </remarks>
		internal void RegisterPropertyChangedCallbackStrong(ExplicitPropertyChangedCallback handler)
		{
			_genericCallbacks = _genericCallbacks.Add(handler);
		}

		private readonly struct InheritedPropertyChangedCallbackDisposable : IDisposable
		{
			public InheritedPropertyChangedCallbackDisposable(ManagedWeakReference objectStoreWeak, DependencyObjectStore childStore)
			{
				ChildStore = childStore;
				ObjectStoreWeak = objectStoreWeak;
			}

			private readonly DependencyObjectStore ChildStore;
			private readonly ManagedWeakReference ObjectStoreWeak;

			public void Dispose()
				=> CleanupInheritedPropertyChangedCallback(ObjectStoreWeak, ChildStore);
		}

		/// <summary>
		/// Register for changes all dependency properties changes notifications for the specified instance.
		/// </summary>
		/// <param name="instance">The instance for which to observe properties changes</param>
		/// <param name="callback">The callback</param>
		/// <returns>A disposable that will unregister the callback when disposed.</returns>
		private InheritedPropertyChangedCallbackDisposable RegisterInheritedPropertyChangedCallback(DependencyObjectStore childStore)
		{
			_childrenStores = _childrenStores.Add(childStore);

			PropagateInheritedProperties(childStore);

			// This weak reference ensure that the disposable will not link
			// the caller and the callee, in the same way "newValueActionWeak"
			// does not link the callee to the caller.
			var objectStoreWeak = ThisWeakReference;

			return new InheritedPropertyChangedCallbackDisposable(objectStoreWeak, childStore);
		}

		private static void CleanupInheritedPropertyChangedCallback(ManagedWeakReference objectStoreWeak, DependencyObjectStore childStore)
		{
			if (objectStoreWeak.Target is DependencyObjectStore that)
			{
				// Delegates integrate a null check when removing new delegates.
				that._childrenStores = that._childrenStores.Remove(childStore);
			}
		}

		/// <summary>
		/// Registers a parent changed callback for itself
		/// </summary>
		/// <param name="callback">A callback used to notified that the parent changed</param>
		/// <remarks>The <see cref="ParentChangedCallback"/> key parameter will always be null</remarks>
		internal void RegisterSelfParentChangedCallback(ParentChangedCallback callback)
		{
			_parentChangedCallbacks = _parentChangedCallbacks.Add(callback);
		}

		/// <summary>
		/// Registers to parent changes.
		/// </summary>
		/// <param name="key">A key to be passed to the callback parameter.</param>
		/// <param name="callback">A callback to be called</param>
		internal IDisposable RegisterParentChangedCallback(object key, ParentChangedCallback callback)
		{
			var wr = WeakReferencePool.RentWeakReference(this, callback);

			ParentChangedCallback weakDelegate =
				(s, _, e) => (wr.Target as ParentChangedCallback)?.Invoke(s, key, e);

			_parentChangedCallbacks = _parentChangedCallbacks.Add(weakDelegate);

			// This weak reference ensure that the closure will not link
			// the caller and the callee, in the same way "newValueActionWeak"
			// does not link the callee to the caller.
			var instanceRef = ThisWeakReference;

			void Cleanup()
			{
				var that = instanceRef.Target as DependencyObjectStore;

				if (that != null)
				{
					// Delegates integrate a null check when removing new delegates.
					that._parentChangedCallbacks = that._parentChangedCallbacks.Remove(weakDelegate);
				}

				WeakReferencePool.ReturnWeakReference(that, wr);

				// Force a closure on the callback, to make its lifetime as long
				// as the subscription being held by the callee.
				callback = null!;
			}

			return new DispatcherConditionalDisposable(
				callback.Target,
				instanceRef.CloneWeakReference(),
				Cleanup
			);
		}

		internal (object? value, DependencyPropertyValuePrecedences precedence) GetValueUnderPrecedence(DependencyProperty property, DependencyPropertyValuePrecedences precedence)
		{
			var stack = _properties.GetPropertyDetails(property);

			return stack.GetValueUnderPrecedence(precedence);
		}

		internal DependencyPropertyDetails GetPropertyDetails(DependencyProperty property)
		{
			return _properties.GetPropertyDetails(property);
		}

		// Keep a list of inherited properties that have been updated so they can be reset.
		HashSet<DependencyProperty> _updatedProperties = new HashSet<DependencyProperty>(DependencyPropertyComparer.Default);

		private void OnParentPropertyChangedCallback(ManagedWeakReference sourceInstance, DependencyProperty parentProperty, DependencyPropertyChangedEventArgs args)
		{
			var (localProperty, propertyDetails) = GetLocalPropertyDetails(parentProperty);

			if (localProperty != null)
			{
				// If the property is available on the current DependencyObject, update it.
				// This will allow for it to be reset to is previous lower precedence.
				if (
					localProperty != _dataContextProperty &&
					localProperty != _templatedParentProperty &&
					!_updatedProperties.Contains(localProperty)
				)
				{
					_updatedProperties.Add(localProperty);
				}

				SetValue(localProperty, args.NewValue, DependencyPropertyValuePrecedences.Inheritance, propertyDetails);
			}
			else
			{
				// Always update the inherited properties with the new value, the instance
				// may change if a far ancestor changed.
				_inheritedForwardedProperties[parentProperty] = sourceInstance;

				// If not, propagate the DP down to the child listeners, if any.
				for (var storeIndex = 0; storeIndex < _childrenStores.Count; storeIndex++)
				{
					var child = _childrenStores[storeIndex];
					child.OnParentPropertyChangedCallback(sourceInstance, parentProperty, args);
				}
			}
		}

		private void TryRegisterInheritedProperties(IDependencyObjectStoreProvider? parentProvider = null, bool force = false)
		{
			if (
				!_registeringInheritedProperties
				&& !_unregisteringInheritedProperties
				&& _inheritedProperties.Disposable == null
				&& (
					IsAutoPropertyInheritanceEnabled
					|| force

					// these cases may be required in case the
					// graph is built in reverse (such as with the
					// XamlReader)
					|| _properties.HasBindings
					|| _childrenStores.Count != 0
				)
			)
			{
				if (parentProvider == null && Parent is IDependencyObjectStoreProvider p)
				{
					parentProvider = p;
				}

				if (parentProvider != null)
				{
#if !HAS_EXPENSIVE_TRYFINALLY
					// The try/finally incurs a very large performance hit in mono-wasm, and SetValue is in a very hot execution path.
					// See https://github.com/dotnet/runtime/issues/50783 for more details.
					try
#endif
					{
						_registeringInheritedProperties = true;

						_inheritedProperties.Disposable = RegisterInheritedProperties(parentProvider);
					}
#if !HAS_EXPENSIVE_TRYFINALLY
					finally
#endif
					{
						_registeringInheritedProperties = false;
					}
				}
			}
		}

		private readonly struct InheritedPropertiesDisposable : IDisposable
		{
			private readonly IDisposable _inheritedPropertiesCallback;
			private readonly DependencyObjectStore _owner;

			public InheritedPropertiesDisposable(DependencyObjectStore owner, IDisposable inheritedPropertiesCallback)
			{
				_owner = owner;
				_inheritedPropertiesCallback = inheritedPropertiesCallback;
			}

			public void Dispose()
			{
				_inheritedPropertiesCallback.Dispose();
				_owner.CleanupInheritedProperties();
			}
		}

		private InheritedPropertiesDisposable RegisterInheritedProperties(IDependencyObjectStoreProvider parentProvider)
		{
			_parentTemplatedParentProperty = parentProvider.Store.TemplatedParentProperty;
			_parentDataContextProperty = parentProvider.Store.DataContextProperty;

			// The propagation of the inherited properties is performed by setting the
			// Inherited precedence level value of each control of the visual tree.
			// This is performed in three ways:
			//    - By listening to the parent's property changes,
			//    - By replicating the parent's current state when a DependencyObject parent's is set
			//    - By forcing a property update notification down to a DependencyObject's children when the parent is set.

			// Subscribe to the parent's notifications
			var inheritedPropertiesCallback = parentProvider
				.Store
				.RegisterInheritedPropertyChangedCallback(this);

			// Force propagation for inherited properties defined on the current instance.
			PropagateInheritedProperties();

			return new InheritedPropertiesDisposable(this, inheritedPropertiesCallback);
		}

		private void CleanupInheritedProperties()
		{
#if !HAS_EXPENSIVE_TRYFINALLY
			// The try/finally incurs a very large performance hit in mono-wasm, and SetValue is in a very hot execution path.
			// See https://github.com/dotnet/runtime/issues/50783 for more details.
			try
#endif
			{
				_unregisteringInheritedProperties = true;

				_inheritedForwardedProperties.Clear();

				if (ActualInstance != null)
				{
					foreach (var dp in _updatedProperties)
					{
						SetValue(dp, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Inheritance);
					}

					SetValue(_dataContextProperty!, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Inheritance);
					SetValue(_templatedParentProperty!, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Inheritance);
				}
			}
#if !HAS_EXPENSIVE_TRYFINALLY
			finally
#endif
			{
				_unregisteringInheritedProperties = false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (DependencyProperty? localProperty, DependencyPropertyDetails? propertyDetails) GetLocalPropertyDetails(DependencyProperty property)
		{
			if (_parentDataContextProperty.UniqueId == property.UniqueId)
			{
				return (_dataContextProperty, _properties.DataContextPropertyDetails);
			}
			else if (_parentTemplatedParentProperty == property)
			{
				return (_templatedParentProperty, _properties.TemplatedParentPropertyDetails);
			}
			else
			{
				// Look for a property with the same name, even if it is not of the same type
				var localProperty = DependencyProperty.GetProperty(_originalObjectType, property.Name);

				if (localProperty != null)
				{
					var propertyDetails = _properties.GetPropertyDetails(localProperty);

					if (HasInherits(propertyDetails))
					{
						return (localProperty, propertyDetails);
					}
				}

				// Then look for attached inheritable properties, only if a property details
				// has been initialized. This avoids creating the details if the property has
				// not been attached on a child, or if there's no property changed callback
				// attached to a child.
				else if (
					property.IsAttached
					&& _properties.FindPropertyDetails(property) is DependencyPropertyDetails attachedDetails
					&& HasInherits(attachedDetails)
				)
				{
					return (property, attachedDetails);
				}
			}

			return (null, null);
		}

		/// <summary>
		/// Do a tree walk to find the correct values of StaticResource and ThemeResource assignations.
		/// </summary>
		internal void UpdateResourceBindings(bool isThemeChangedUpdate, ResourceDictionary? containingDictionary = null)
		{
			if (_resourceBindings == null || !_resourceBindings.HasBindings)
			{
				UpdateChildResourceBindings(isThemeChangedUpdate);
				return;
			}

			var dictionariesInScope = GetResourceDictionaries(includeAppResources: false, containingDictionary).ToArray();

			var bindings = _resourceBindings.GetAllBindings().ToList(); //The original collection may be mutated during DP assignations

			foreach (var (property, binding) in bindings)
			{
				try
				{
					var wasSet = false;
					foreach (var dict in dictionariesInScope)
					{
						if (dict.TryGetValue(binding.ResourceKey, out var value, shouldCheckSystem: false))
						{
							wasSet = true;
							SetResourceBindingValue(property, binding, value);
							break;
						}
					}

					if (!wasSet && isThemeChangedUpdate && binding.IsThemeResourceExtension)
					{
						if (ResourceResolver.TryTopLevelRetrieval(binding.ResourceKey, binding.ParseContext, out var value))
						{
							SetResourceBindingValue(property, binding, value);
						}
					}
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Warning))
					{
						this.Log().Warn($"Failed to update binding, target may have been disposed", e);
					}
				}
			}

			UpdateChildResourceBindings(isThemeChangedUpdate);
		}

		private void SetResourceBindingValue(DependencyProperty property, ResourceBinding binding, object? value)
		{
			var convertedValue = BindingPropertyHelper.Convert(() => property.Type, value);
			if (binding.SetterBindingPath != null)
			{
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
				try
#endif
				{
					_isSettingThemeBinding = binding.IsThemeResourceExtension;
					binding.SetterBindingPath.Value = convertedValue;
				}
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
				finally
#endif
				{
					_isSettingThemeBinding = false;
				}
			}
			else
			{
				SetValue(property, convertedValue, binding.Precedence, isThemeBinding: binding.IsThemeResourceExtension);
			}
		}

		private bool _isUpdatingChildResourceBindings;

		private void UpdateChildResourceBindings(bool isThemeChangedUpdate)
		{
			if (_isUpdatingChildResourceBindings)
			{
				// Some DPs might be creating reference cycles, so we make sure not to enter an infinite loop.
				return;
			}
			if (isThemeChangedUpdate)
			{
				try
				{
					_isUpdatingChildResourceBindings = true;
					foreach (var child in GetChildrenDependencyObjects())
					{
						if (!(child is IFrameworkElement) && child is IDependencyObjectStoreProvider storeProvider)
						{
							storeProvider.Store.UpdateResourceBindings(isThemeChangedUpdate);
						}
					}
				}
				finally
				{
					_isUpdatingChildResourceBindings = false;
				}

				if (ActualInstance is IThemeChangeAware themeChangeAware)
				{
					// Call OnThemeChanged after bindings of descendants have been updated
					themeChangeAware.OnThemeChanged();
				}
			}
		}

		/// <summary>
		/// Returns all discoverable child dependency objects.
		/// </summary>
		/// <remarks>
		/// This method is potentially slow and should only be used where performance isn't a concern (eg updating resource bindings
		/// when the app theme changes).
		/// </remarks>
		private IEnumerable<DependencyObject> GetChildrenDependencyObjects()
		{
			var propertyValues = _properties.GetAllDetails()
				.Except(_properties.DataContextPropertyDetails, _properties.TemplatedParentPropertyDetails)
				.Select(d => GetValue(d));
			foreach (var propertyValue in propertyValues)
			{
				if (propertyValue is IEnumerable<DependencyObject> dependencyObjectCollection &&
					// Try to avoid enumerating collections that shouldn't be enumerated, since we may be encountering user-defined values. This may need to be refined to somehow only consider values coming from the framework itself.
					(propertyValue is ICollection || propertyValue is DependencyObjectCollectionBase)
				)
				{
					foreach (var innerValue in dependencyObjectCollection)
					{
						yield return innerValue;
					}
				}

				if (propertyValue is IAdditionalChildrenProvider updateable)
				{
					foreach (var innerValue in updateable.GetAdditionalChildObjects())
					{
						yield return innerValue;
					}
				}

				if (propertyValue is DependencyObject dependencyObject)
				{
					yield return dependencyObject;
				}
			}
		}

		/// <summary>
		/// Returns all ResourceDictionaries in scope using the visual tree, from nearest to furthest.
		/// </summary>
		private IEnumerable<ResourceDictionary> GetResourceDictionaries(bool includeAppResources, ResourceDictionary? containingDictionary = null)
		{
			if (containingDictionary != null)
			{
				yield return containingDictionary;
			}

			var candidate = ActualInstance;
			var candidateFE = candidate as FrameworkElement;

			while (candidate != null)
			{
				var parent = candidate.GetParent() as DependencyObject;

				if (candidateFE != null)
				{
					yield return candidateFE.Resources;

					if (parent is FrameworkElement fe)
					{
						// If the parent is a framework element, cast only once and assign
						// the result to both variables.
						candidate = candidateFE = fe;
					}
					else
					{
						candidate = parent;
					}
				}
				else
				{
					candidateFE = parent as FrameworkElement;
					candidate = parent;
				}
			}
			if (includeAppResources && Application.Current != null)
			{
				// In the case of StaticResource resolution we skip Application.Resources because we assume these were already checked at initialize-time.
				yield return Application.Current.Resources;
			}
		}

		/// <summary>
		/// Retrieve the implicit Style for <see cref="ActualInstance"/> by walking the visual tree.
		/// </summary>
		internal Style? GetImplicitStyle(in SpecializedResourceDictionary.ResourceKey styleKey)
		{
			foreach (var dict in GetResourceDictionaries(includeAppResources: true))
			{
				if (dict.TryGetValue(styleKey, out var style, shouldCheckSystem: false))
				{
					return style as Style;
				}
			}

			return null;
		}

		/// <summary>
		/// Propagate the current inheritable properties to the registered children.
		/// </summary>
		internal void PropagateInheritedProperties(DependencyObjectStore? childStore = null)
		{
			// Raise the property change for the current values
			var props = DependencyProperty.GetFrameworkPropertiesForType(_originalObjectType, FrameworkPropertyMetadataOptions.Inherits);

			// Not using the ActualInstance property here because we need to get a WeakReference instead.
			var instanceRef = _originalObjectRef != null ? _originalObjectRef : ThisWeakReference;

			void Propagate(DependencyObjectStore store)
			{
				for (var propertyIndex = 0; propertyIndex < props.Length; propertyIndex++)
				{
					var prop = props[propertyIndex];

					store.OnParentPropertyChangedCallback(instanceRef, prop, new DependencyPropertyChangedEventArgs(
						prop,
						null,
						DependencyPropertyValuePrecedences.DefaultValue,
						GetValue(prop),
						DependencyPropertyValuePrecedences.Inheritance
					));
				}
			}

			if (childStore != null)
			{
				Propagate(childStore);
			}
			else
			{
				for (var childStoreIndex = 0; childStoreIndex < _childrenStores.Count; childStoreIndex++)
				{
					var child = _childrenStores[childStoreIndex];
					Propagate(child);
				}
			}

			PropagateInheritedNonLocalProperties(childStore);
		}

		private void PropagateInheritedNonLocalProperties(DependencyObjectStore? childStore)
		{
			// Propagate the properties that have been inherited from an other
			// parent, but that are not defined in the current instance.
			// This is used when a child is being added after the parent has already set its inheritable
			// properties.

			// Ancestors is a local cache to avoid walking up the tree multiple times.
			var ancestors = new Dictionary<object, bool>();

			// This alias is used to avoid the resolution of the underlying WeakReference during the
			// call to IsAncestor.
			var actualInstanceAlias = ActualInstance;

			foreach (var sourceInstanceProperties in _inheritedForwardedProperties)
			{
				if (
					IsAncestor(actualInstanceAlias, ancestors, sourceInstanceProperties.Value.Target)
					|| (
						sourceInstanceProperties.Key.IsAttached
						&& actualInstanceAlias == sourceInstanceProperties.Value.Target
					)
				)
				{
					void Propagate(DependencyObjectStore store)
					{
						store.OnParentPropertyChangedCallback(
							sourceInstanceProperties.Value,
							sourceInstanceProperties.Key,
							new DependencyPropertyChangedEventArgs(
								sourceInstanceProperties.Key,
								null,
								DependencyPropertyValuePrecedences.DefaultValue,

								// Get the value from the parent that holds the master value
								(sourceInstanceProperties.Value.Target as DependencyObject)?.GetValue(sourceInstanceProperties.Key),
								DependencyPropertyValuePrecedences.Inheritance
							)
						);
					}

					if (childStore != null)
					{
						Propagate(childStore);
					}
					else
					{
						foreach (var child in _childrenStores)
						{
							Propagate(child);
						}
					}
				}
			}
		}

		private static bool IsAncestor(DependencyObject? instance, Dictionary<object, bool> map, object ancestor)
		{
#if DEBUG
			var hashSet = new HashSet<DependencyObject>(Uno.ReferenceEqualityComparer<DependencyObject>.Default);
			if (instance != null)
			{
				hashSet.Add(instance);
			}
#endif

			bool isAncestor = false;

			if (ancestor != null && !map.TryGetValue(ancestor, out isAncestor))
			{
				while (instance != null)
				{
#if DEBUG
					var prevInstance = instance;
#endif
					instance = DependencyObjectExtensions.GetParent(instance) as DependencyObject;

#if DEBUG
					if (instance != null)
					{
						if (!hashSet.Contains(instance!))
						{
							// Console.WriteLine($"Added other {(instance as FrameworkElement)?.Name}");
							hashSet.Add(instance);
						}
						else
						{
							throw new Exception($"Cycle detected: [{prevInstance}/{(prevInstance as FrameworkElement)?.Name}] has already added [{instance}/{(instance as FrameworkElement)?.Name}] as parent/");
						}
					}
#endif

					if (instance == ancestor)
					{
						isAncestor = true;
					}

				}

				map[ancestor] = isAncestor;
			}

			return isAncestor;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool HasInherits(DependencyPropertyDetails propertyDetails)
		{
			var metadata = propertyDetails.Metadata;

			if (metadata is FrameworkPropertyMetadata frameworkMetadata)
			{
				return frameworkMetadata.Options.HasInherits();
			}

			return false;
		}

		public DependencyObject? ActualInstance
			=> _hardOriginalObjectRef ?? _originalObjectRef.Target as DependencyObject;

		/// <summary>
		/// Creates a weak delegate for the specified PropertyChangedCallback callback.
		/// </summary>
		/// <param name="callback">The callback to reference</param>
		/// <remarks>
		/// This method is used to avoid creating a hard link between the source instance
		/// and the stored delegate for the instance, thus avoid memory leaks.
		/// We also do not need to clear the delegate created because it is already associated with the instance.
		///
		/// Note that this method is not generic to avoid the cost of trampoline resolution
		/// on Mono 4.2 and earlier, when Full AOT is enabled. This should be revised once this behavior is updated, or
		/// the cost of calling generic delegates is lowered.
		/// </remarks>
		private static (PropertyChangedCallback callback, IDisposable release) CreateWeakDelegate(PropertyChangedCallback callback)
		{
			var wr = WeakReferencePool.RentWeakReference(null, callback);

			PropertyChangedCallback weakDelegate =
				(s, e) => (!wr.IsDisposed ? wr.Target as PropertyChangedCallback : null)?.Invoke(s, e);

			return (weakDelegate, Disposable.Create(() => WeakReferencePool.ReturnWeakReference(null, wr)));
		}

		/// <summary>
		/// Creates a weak delegate for the specified <see cref="Action"/> callback.
		/// </summary>
		/// <param name="callback">The callback to reference</param>
		/// <remarks>
		/// This method is used to avoid creating a hard link between the source instance
		/// and the stored delegate for the instance, thus avoid memory leaks.
		/// We also do not need to clear the delegate created because it is already associated with the instance.
		///
		/// Note that this method is not generic to avoid the cost of trampoline resolution
		/// on Mono 4.2 and earlier, when Full AOT is enabled. This should be revised once this behavior is updated, or
		/// the cost of calling generic delegates is lowered.
		/// </remarks>
		private static (Action callback, IDisposable release) CreateWeakDelegate(Action callback)
		{
			var wr = WeakReferencePool.RentWeakReference(null, callback);

			Action weakDelegate =
				() => (wr.Target as Action)?.Invoke();

			return (weakDelegate, Disposable.Create(() => WeakReferencePool.ReturnWeakReference(null, wr)));
		}

		/// <summary>
		/// Creates a weak delegate for the specified ExplicitPropertyChangedCallback callback.
		/// </summary>
		/// <param name="callback">The callback to reference</param>
		/// <remarks>
		/// This method is used to avoid creating a hard link between the source instance
		/// and the stored delegate for the instance, thus avoid memory leaks.
		/// We also do not need to clear the delegate created because it is already associated with the instance.
		///
		/// Note that this method is not generic to avoid the cost of trampoline resolution
		/// on Mono 4.2 and earlier, when Full AOT is enabled. This should be revised once this behavior is updated, or
		/// the cost of calling generic delegates is lowered.
		/// </remarks>
		private static (ExplicitPropertyChangedCallback callback, IDisposable release) CreateWeakDelegate(ExplicitPropertyChangedCallback callback)
		{
			var wr = WeakReferencePool.RentWeakReference(null, callback);

			ExplicitPropertyChangedCallback weakDelegate =
				(instance, s, e) => (wr.Target as ExplicitPropertyChangedCallback)?.Invoke(instance, s, e);

			return (weakDelegate, Disposable.Create(() => WeakReferencePool.ReturnWeakReference(null, wr)));
		}

		private void RaiseCallbacks(
			DependencyObject actualInstanceAlias,
			DependencyPropertyDetails propertyDetails,
			object? previousValue,
			DependencyPropertyValuePrecedences previousPrecedence,
			object? newValue,
			DependencyPropertyValuePrecedences newPrecedence
		)
		{
			var hasPropagationBypass = _propagationBypass.Count != 0 || _propagationBypassed.Count != 0;

			// This check is present to avoid allocating if there is no bypass.
			var propertyPath = hasPropagationBypass ? new DependencyPropertyPath(actualInstanceAlias, propertyDetails.Property) : null;

			if (AreDifferent(newValue, previousValue))
			{
				var bypassesPropagation = hasPropagationBypass && _propagationBypass.Contains(propertyPath!);

				if (bypassesPropagation)
				{
					_propagationBypassed[propertyPath!] = previousValue;
				}

				InvokeCallbacks(actualInstanceAlias, propertyDetails.Property, propertyDetails, previousValue, previousPrecedence, newValue, newPrecedence, bypassesPropagation);
			}
			else if (
				hasPropagationBypass
				&& _propagationBypassed.ContainsKey(propertyPath!)
				&& !_propagationBypass.Contains(propertyPath, DependencyPropertyPath.Comparer.Default)
			)
			{
				// If unchanged, but previous value was set with propagation bypass enabled (and we are currently being set without bypass enabled),
				// then we should invoke callbacks so that the value can be propagated. This arises in animation scenarios.
				var unpropagatedPrevious = _propagationBypassed[propertyPath!];
				_propagationBypassed.Remove(propertyPath!);

				InvokeCallbacks(actualInstanceAlias, propertyDetails.Property, propertyDetails, unpropagatedPrevious, previousPrecedence, newValue, newPrecedence);
			}
			else if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug(
					$"Skipped raising PropertyChangedCallbacks because value for property {propertyDetails.Property.OwnerType}.{propertyDetails.Property.Name} remained identical."
				);
			}
		}

		private void InvokeCallbacks(
			DependencyObject actualInstanceAlias,
			DependencyProperty property,
			DependencyPropertyDetails propertyDetails,
			object? previousValue,
			DependencyPropertyValuePrecedences previousPrecedence,
			object? newValue,
			DependencyPropertyValuePrecedences newPrecedence,
			bool bypassesPropagation = false
		)
		{
			var eventArgs = new DependencyPropertyChangedEventArgs(property, previousValue, previousPrecedence, newValue, newPrecedence, bypassesPropagation);
			var propertyMetadata = propertyDetails.Metadata;

			// We can reuse the weak reference, otherwise capture the weak reference to this instance.
			var instanceRef = _originalObjectRef ?? ThisWeakReference;

			if (propertyMetadata is FrameworkPropertyMetadata frameworkPropertyMetadata)
			{
				if (frameworkPropertyMetadata.Options.HasLogicalChild())
				{
					UpdateAutoParent(actualInstanceAlias, previousValue, newValue);
				}

				if (frameworkPropertyMetadata.Options.HasAffectsMeasure())
				{
					actualInstanceAlias.InvalidateMeasure();
				}

				if (frameworkPropertyMetadata.Options.HasAffectsArrange())
				{
					actualInstanceAlias.InvalidateArrange();
				}

				if (frameworkPropertyMetadata.Options.HasAffectsRender())
				{
					actualInstanceAlias.InvalidateRender();
				}

				// Raise the property change for generic handlers for inheritance
				if (frameworkPropertyMetadata.Options.HasInherits())
				{
					for (var storeIndex = 0; storeIndex < _childrenStores.Count; storeIndex++)
					{
						CallChildCallback(_childrenStores[storeIndex], instanceRef, property, eventArgs);
					}
				}
			}
			else
			{
				if (property.IsDependencyObjectCollection)
				{
					// Force the parent of a DependencyObjectCollection to the current instance/
					// There is no public UWP API that allows to set the parent of a DependencyObject,
					// even though defining a DependencyObjectCollection dependency property automatically propagates
					// the compiled and normal bindings properly. We emulate this behavior here, but only if the
					// metadata is PropertyMetadata, not FrameworkPropertyMetadata.
					UpdateAutoParent(actualInstanceAlias, previousValue, newValue);
				}
			}

			// Raise the callback for backing fields update before PropertyChanged to get
			// the backingfield updated, in case the PropertyChanged handler reads the
			// dependency property value through the cache.
			propertyMetadata.RaiseBackingFieldUpdate(actualInstanceAlias, newValue);

			// Raise the changes for the callback register to the property itself
			propertyMetadata.RaisePropertyChanged(actualInstanceAlias, eventArgs);

			// Raise the common property change callback of WinUI
			if (actualInstanceAlias is UIElement uiElt)
			{
				uiElt.OnPropertyChanged2(eventArgs);
			}

			// Raise the changes for the callbacks register through RegisterPropertyChangedCallback.
			propertyDetails.CallbackManager.RaisePropertyChanged(actualInstanceAlias, eventArgs);

			OnDependencyPropertyChanged(propertyDetails, eventArgs);

			// Raise the property change for generic handlers
			for (var callbackIndex = 0; callbackIndex < _genericCallbacks.Data.Length; callbackIndex++)
			{
				var callback = _genericCallbacks.Data[callbackIndex];
				callback.Invoke(instanceRef, property, eventArgs);
			}
		}

		private void CallChildCallback(DependencyObjectStore childStore, ManagedWeakReference instanceRef, DependencyProperty property, DependencyPropertyChangedEventArgs eventArgs)
		{
			var propagateUnregistering = (_unregisteringInheritedProperties || _parentUnregisteringInheritedProperties) && property == _dataContextProperty;
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
			try
#endif
			{
				if (propagateUnregistering)
				{
					childStore._parentUnregisteringInheritedProperties = true;
				}
				childStore.OnParentPropertyChangedCallback(instanceRef, property, eventArgs);
			}
#if !HAS_EXPENSIVE_TRYFINALLY // Try/finally incurs a very large performance hit in mono-wasm - https://github.com/dotnet/runtime/issues/50783
			finally
#endif
			{
				if (propagateUnregistering)
				{
					childStore._parentUnregisteringInheritedProperties = false;
				}
			}
		}

		/// <summary>
		/// Updates the parent of the <paramref name="newValue"/> to the
		/// <paramref name="actualInstanceAlias"/> and resets the parent of <paramref name="previousValue"/>.
		/// </summary>
		private static void UpdateAutoParent(DependencyObject actualInstanceAlias, object? previousValue, object? newValue)
		{
			if (
				previousValue is DependencyObject previousObject
				&& ReferenceEquals(previousObject.GetParent(), actualInstanceAlias)
			)
			{
				previousObject.SetParent(null);
			}

			if (newValue is DependencyObject newObject)
			{
				newObject.SetParent(actualInstanceAlias);
			}
		}

		/// <summary>
		/// Sets the value in the proper layer of the value precedence stack
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="value">The value to set</param>
		/// <param name="precedence">The value precedence to assign</param>
		private void SetValueInternal(
			object? value,
			DependencyPropertyValuePrecedences precedence,
			DependencyPropertyDetails propertyDetails
		)
		{
			if (value != null
				&& value != DependencyProperty.UnsetValue
				&& ((propertyDetails.Metadata as FrameworkPropertyMetadata)?.Options.HasAutoConvert() ?? false))
			{
				if (value?.GetType() != propertyDetails.Property.Type)
				{
					value = Convert.ChangeType(value, propertyDetails.Property.Type);
				}
			}

			if (AreDifferent(value, propertyDetails.GetValue(precedence)))
			{
				propertyDetails.SetValue(value, precedence);
			}
		}

		/// <summary>
		/// Determines if the two values are different, based on the type of the objects.
		/// </summary>
		/// <param name="previousValue">The previous value</param>
		/// <param name="newValue">The new value</param>
		/// <returns>True if different, otherwise false</returns>
		/// <remarks>This comparison uses value for value types, references for reference types.</remarks>
		public static bool AreDifferent(object? previousValue, object? newValue)
		{
			if (newValue is ValueType || newValue is string)
			{
				return !object.Equals(previousValue, newValue);
			}
			else
			{
				return !object.ReferenceEquals(previousValue, newValue);
			}
		}

		private void OnParentChanged(object? previousParent, object? value)
		{
			if (_parentChangedCallbacks.Data.Length != 0)
			{
				var actualInstanceAlias = ActualInstance;

				if (actualInstanceAlias != null)
				{
					var args = new DependencyObjectParentChangedEventArgs(previousParent, value);

					for (var parentCallbackIndex = 0; parentCallbackIndex < _parentChangedCallbacks.Data.Length; parentCallbackIndex++)
					{
						var handler = _parentChangedCallbacks.Data[parentCallbackIndex];
						handler.Invoke(actualInstanceAlias, null, args);
					}
				}
			}

			CheckThemeBindings(previousParent, value);
		}

		/// <summary>
		/// If we're being unloaded, save the current theme. If we're being loaded, check if application theme has changed since theme
		/// bindings were last applied, and update if needed.
		/// </summary>
		private void CheckThemeBindings(object? previousParent, object? value)
		{
			if (ActualInstance is FrameworkElement frameworkElement)
			{
				if (value == null && previousParent != null)
				{
					_themeLastUsed = Application.Current?.RequestedThemeForResources;
				}
				else if (previousParent == null && value != null && _themeLastUsed is { } previousTheme)
				{
					_themeLastUsed = null;
					if (Application.Current?.RequestedThemeForResources is { } currentTheme && !previousTheme.Equals(currentTheme))
					{
						Application.PropagateThemeChanged(frameworkElement);
					}
				}
			}
		}

		/// <summary>
		/// Set theme used when applying theme-bound values.
		/// </summary>
		/// <param name="resourceKey">Key for the theme used</param>
		internal void SetLastUsedTheme(SpecializedResourceDictionary.ResourceKey? resourceKey) => _themeLastUsed = resourceKey;

		private ManagedWeakReference ThisWeakReference
			=> _thisWeakRef ??= Uno.UI.DataBinding.WeakReferencePool.RentWeakReference(this, this);

		internal IEnumerable<ResourceBinding> GetResourceBindingsForProperty(DependencyProperty dependencyProperty)
			=> _resourceBindings?.GetBindingsForProperty(dependencyProperty)
				?? Enumerable.Empty<ResourceBinding>();

		/// <summary>
		/// Enables hard references for internal fields for faster access
		/// </summary>
		/// <remarks>
		/// Calling this method may cause memory leaks and must be used along with <see cref="DisableHardReferences"/>.
		/// The use case is for FrameworkElement instances that can be Loaded and Unloaded, and for which the Unloaded
		/// state is ensured to happen in such a way that memory leaks cannot happen.
		/// </remarks>
		internal void TryEnableHardReferences()
		{
			if (FeatureConfiguration.DependencyObject.IsStoreHardReferenceEnabled)
			{
				_hardParentRef = Parent;
				_hardOriginalObjectRef = ActualInstance;

				_properties.TryEnableHardReferences();
			}
		}

		/// <summary>
		/// Disables hard references for internal fields created by <see cref="EnableHardReferences"/>
		/// </summary>
		internal void DisableHardReferences()
		{
			if (FeatureConfiguration.DependencyObject.IsStoreHardReferenceEnabled)
			{
				_hardParentRef = null;
				_hardOriginalObjectRef = null;

				_properties.DisableHardReferences();
			}
		}

		/// <summary>
		/// Determines if <see cref="EnableHardReferences"/> has been called
		/// </summary>
		internal bool AreHardReferencesEnabled => _hardParentRef != null;

		private class DependencyPropertyPath : IEquatable<DependencyPropertyPath?>
		{
			public DependencyPropertyPath(DependencyObject instance, DependencyProperty property)
			{
				Instance = instance;
				Property = property;
			}

			public DependencyObject Instance { get; }

			public DependencyProperty Property { get; }

			public override int GetHashCode()
				=> Instance.GetHashCode() ^
					Property.UniqueId.GetHashCode();

			public override bool Equals(object obj)
				=> Equals(obj as DependencyPropertyPath);

			public bool Equals(DependencyPropertyPath? other)
				=> other != null &&
					ReferenceEquals(this.Instance, other.Instance) &&
					this.Property.UniqueId == other.Property.UniqueId;

			public class Comparer : IEqualityComparer<DependencyPropertyPath?>
			{
				public static Comparer Default { get; } = new Comparer();

				private Comparer()
				{
				}

				public bool Equals(DependencyPropertyPath? x, DependencyPropertyPath? y)
					=> x?.Equals(y) ?? false;

				public int GetHashCode(DependencyPropertyPath? obj)
					=> obj?.GetHashCode() ?? 0;
			}
		}

		/// <summary>
		/// A comparer that compares references of the target values.|
		/// </summary>
		private class WeakReferenceValueComparer : IEqualityComparer<WeakReference>
		{
			public static readonly WeakReferenceValueComparer Default = new WeakReferenceValueComparer();

			public bool Equals(WeakReference x, WeakReference y)
			{
				return ReferenceEquals(x?.Target, y?.Target);
			}

			public int GetHashCode(WeakReference obj)
			{
				return obj?.GetHashCode() ?? 0;
			}
		}
	}
}
