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
	internal delegate void ParentChangedCallback(object instance, object key, DependencyObjectParentChangedEventArgs args);

	/// <summary>
	/// Defines a Dependency Object
	/// </summary>
	public partial class DependencyObjectStore : IDisposable
	{
		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{430FC851-E917-4587-AF7B-5A1CE5A1941D}");
			public const int GetValue = 1;
			public const int SetValueStart = 2;
			public const int SetValueStop = 3;
			public const int CreationTask = 4;
			public const int DataContextChangedStart = 5;
			public const int DataContextChangedStop = 6;
		}

		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);


		/// <summary>
		/// A global static counter that is used to uniquely identify objects.
		/// This is primarily used during trace profiling.
		/// </summary>
		private static long _objectIdCounter;

		private bool _isDisposed;

		private readonly DependencyPropertyDetailsCollection _properties;
		private readonly DependencyPropertyDetails _dataContextPropertyDetails;
		private readonly DependencyPropertyDetails _templatedParentPropertyDetails;

		private DependencyProperty _parentTemplatedParentProperty;
		private DependencyProperty _parentDataContextProperty;

		private ImmutableList<ExplicitPropertyChangedCallback> _genericCallbacks = ImmutableList<ExplicitPropertyChangedCallback>.Empty;
		private ImmutableList<DependencyObjectStore> _childrenStores = ImmutableList<DependencyObjectStore>.Empty;
		private ImmutableList<ParentChangedCallback> _parentChangedCallbacks = ImmutableList<ParentChangedCallback>.Empty;
		private ImmutableList<Action> _compiledBindingsCallbacks = ImmutableList<Action>.Empty;

		private readonly ManagedWeakReference _originalObjectRef;

		/// <summary>
		/// This field is used to pass a reference to itself in the case
		/// of DependencyProperty changed registrations. This avoids creating many
		/// weak references to the same object.
		/// </summary>
		private readonly ManagedWeakReference _thisWeakRef;

		private readonly Type _originalObjectType;
		private SerialDisposable _inheritedProperties = new SerialDisposable();
		private SerialDisposable _compiledBindings = new SerialDisposable();
		private ManagedWeakReference _parentRef;
		private Dictionary<DependencyProperty, ManagedWeakReference> _inheritedForwardedProperties = new Dictionary<DependencyProperty, ManagedWeakReference>(DependencyPropertyComparer.Default);
		private DependencyPropertyValuePrecedences? _precedenceOverride;

		private static long _propertyChangedToken = 0;
		private Dictionary<long, IDisposable> _propertyChangedTokens = new Dictionary<long, IDisposable>();

		private bool _registeringInheritedProperties;
		private bool _unregisteringInheritedProperties;

		private static bool _validatePropertyOwner = Debugger.IsAttached;

		/// <summary>
		/// Provides the parent Dependency Object of this dependency object
		/// </summary>
		/// <remarks>
		/// This property is an <see cref="object"/> as the parent of a <see cref="DependencyObject"/> may
		/// not always be another <see cref="DependencyObject"/>, particularly in the case of the root element.
		/// </remarks>
		public object Parent
		{
			get => _parentRef?.Target;
			set
			{
				if (!ReferenceEquals(_parentRef?.Target, value))
				{
					var previousParent = _parentRef?.Target;

					if (_parentRef != null)
					{
						WeakReferencePool.ReturnWeakReference(this, _parentRef);
					}

					_parentRef = WeakReferencePool.RentWeakReference(this, value);

					_inheritedProperties.Disposable = null;
					_compiledBindings.Disposable = null;

					if (value is IDependencyObjectStoreProvider parentProvider)
					{
						TryRegisterInheritedProperties(parentProvider);
						_compiledBindings.Disposable = RegisterCompiledBindingsUpdates();
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

			_thisWeakRef = Uno.UI.DataBinding.WeakReferencePool.RentWeakReference(this, this);

			_properties = new DependencyPropertyDetailsCollection(_originalObjectType, _originalObjectRef, dataContextProperty, templatedParentProperty);
			_dataContextPropertyDetails = _properties.DataContextPropertyDetails;
			_templatedParentPropertyDetails = _properties.TemplatedParentPropertyDetails;

			InitializeBinder(dataContextProperty, templatedParentProperty);

			if (_trace.IsEnabled)
			{
				_trace.WriteEvent(
					DependencyObjectStore.TraceProvider.CreationTask,
					new object[] { ObjectId, _originalObjectType.Name }
				);
			}
		}

		public void Dispose()
		{
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
		public object GetValue(DependencyProperty property)
		{
			return GetValue(property: property, propertyDetails: null, precedence: null, isPrecedenceSpecific: false);
		}

		/// <summary>
		/// Returns the local value of a dependency property, if a local value is set.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <returns></returns>
		public object ReadLocalValue(DependencyProperty property)
		{
			return GetValue(property, precedence: DependencyPropertyValuePrecedences.Local, isPrecedenceSpecific: true);
		}

		/// <summary>
		/// Returns the local value of a dependency property, if a local value is set.
		/// </summary>
		/// <param name="instance">The instance on which the property is attached</param>
		/// <param name="property">The dependency property to get</param>
		/// <returns></returns>
		public object GetAnimationBaseValue(DependencyProperty property)
		{
			return GetValue(property, precedence: DependencyPropertyValuePrecedences.Local);
		}

		internal object GetValue(DependencyProperty property, DependencyPropertyValuePrecedences? precedence = null, bool isPrecedenceSpecific = false)
		{
			return GetValue(property, null, precedence, isPrecedenceSpecific);
		}

		internal object GetValue(DependencyProperty property, DependencyPropertyDetails propertyDetails, DependencyPropertyValuePrecedences? precedence = null, bool isPrecedenceSpecific = false)
		{
			WritePropertyEventTrace(TraceProvider.GetValue, property, precedence);

			ValidatePropertyOwner(property);

			propertyDetails = propertyDetails ?? _properties.GetPropertyDetails(property);

			return GetValue(propertyDetails, precedence, isPrecedenceSpecific);
		}

		private object GetValue(DependencyPropertyDetails propertyDetails, DependencyPropertyValuePrecedences? precedence = null, bool isPrecedenceSpecific = false)
		{
			if (propertyDetails == _dataContextPropertyDetails || propertyDetails == _templatedParentPropertyDetails)
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
		/// <param name="instance">The instance to override</param>
		/// <param name="precedence">The precedence to set</param>
		/// <returns>A disposable to dispose to cancel the override.</returns>
		internal IDisposable OverrideLocalPrecedence(DependencyPropertyValuePrecedences precedence)
		{
			if (_precedenceOverride != null)
			{
				// Keep the current precedence override, which affects application of styles
				// with BasedOn set.
				return null;
			}
			else
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"OverrideLocalPrecedence({precedence})");
				}

				_precedenceOverride = precedence;

				return Disposable.Create(() =>
				{

					_precedenceOverride = null;

					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug("OverrideLocalPrecedence(None)");
					}
				});
			}
		}

		private static List<DependencyPropertyPath> _propagationBypass =
			new List<DependencyPropertyPath>();

		private static Dictionary<DependencyPropertyPath, object> _propagationBypassed =
			new Dictionary<DependencyPropertyPath, object>(DependencyPropertyPath.Comparer.Default);

		internal static IDisposable BypassPropagation(DependencyObject instance, DependencyProperty property)
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

		internal void SetValue(DependencyProperty property, object value, DependencyPropertyValuePrecedences precedence, DependencyPropertyDetails propertyDetails = null)
		{
			if (_trace.IsEnabled)
			{
				using (WritePropertyEventTrace(TraceProvider.SetValueStart, TraceProvider.SetValueStop, property, precedence))
				{
					InnerSetValue(property, value, precedence, propertyDetails);
				}
			}
			else
			{
				InnerSetValue(property, value, precedence, propertyDetails);
			}

		}

		private void InnerSetValue(DependencyProperty property, object value, DependencyPropertyValuePrecedences precedence, DependencyPropertyDetails propertyDetails)
		{
			if (precedence == DependencyPropertyValuePrecedences.Coercion)
			{
				throw new ArgumentException("SetValue must not be called with precedence DependencyPropertyValuePrecedences.Coercion, as it expects a non-coerced value to function properly.");
			}

			var actualInstanceAlias = ActualInstance;

			if (actualInstanceAlias != null)
			{
				ApplyPrecedenceOverride(ref precedence);

				if ((value is UnsetValue) && precedence == DependencyPropertyValuePrecedences.DefaultValue)
				{
					throw new InvalidOperationException("The default value must be a valid value");
				}

				ValidatePropertyOwner(property);

				// Resolve the stack once for the instance, for performance.
				propertyDetails = propertyDetails ?? _properties.GetPropertyDetails(property);

				var previousValue = GetValue(propertyDetails);
				var previousPrecedence = GetCurrentHighestValuePrecedence(propertyDetails);

				// Set even if they are different to make sure the value is now set on the right precedence
				SetValueInternal(value, precedence, propertyDetails);

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

		private void ApplyCoercion(DependencyObject actualInstanceAlias, DependencyPropertyDetails propertyDetails, object previousValue, object baseValue)
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

		private IDisposable WritePropertyEventTrace(int startEventId, int stopEventId, DependencyProperty property, DependencyPropertyValuePrecedences precedence)
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
		/// <param name="property">The property being set</param>
		/// <param name="value"></param>
		/// <param name="precedence"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ApplyPrecedenceOverride(ref DependencyPropertyValuePrecedences precedence)
		{
			if (_precedenceOverride != null)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug(
						$"Overriding {precedence} precedence with {_precedenceOverride}."
					);
				}

				precedence = _precedenceOverride.Value;
			}
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
			IDisposable registration;

			if (_propertyChangedTokens.TryGetValue(token, out registration))
			{
				registration.Dispose();

				_propertyChangedTokens.Remove(token);
			}
		}

		internal IDisposable RegisterPropertyChangedCallback(DependencyProperty property, PropertyChangedCallback callback, DependencyPropertyDetails propertyDetails = null)
		{
			var weakDelegate = CreateWeakDelegate(callback);

			propertyDetails = propertyDetails ?? _properties.GetPropertyDetails(property);

			var cookie = propertyDetails.CallbackManager.RegisterCallback(weakDelegate.callback);

			// Capture the weak reference to this instance.
			var instanceRef = _thisWeakRef;

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
						callback = null;
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
			var instanceRef = _thisWeakRef;

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
					handler = null;
				}
			);
		}

		/// <summary>
		/// Register for changes all dependency properties changes notifications for the specified instance.
		/// </summary>
		/// <param name="instance">The instance for which to observe properties changes</param>
		/// <param name="callback">The callback</param>
		/// <returns>A disposable that will unregister the callback when disposed.</returns>
		internal IDisposable RegisterInheritedPropertyChangedCallback(DependencyObjectStore childStore)
		{
			_childrenStores = _childrenStores.Add(childStore);

			PropagateInheritedProperties(childStore);

			// This weak reference ensure that the closure will not link
			// the caller and the callee, in the same way "newValueActionWeak"
			// does not link the callee to the caller.
			var instanceRef = _thisWeakRef;

			return Disposable.Create(() =>
			{
				var that = instanceRef.Target as DependencyObjectStore;

				if (that != null)
				{
					// Delegates integrate a null check when removing new delegates.
					that._childrenStores = that._childrenStores.Remove(childStore);
				}
			});
		}


		/// <summary>
		/// Register for changes all dependency properties changes notifications for the specified instance.
		/// </summary>
		/// <param name="instance">The instance for which to observe properties changes</param>
		/// <param name="callback">The callback</param>
		/// <returns>A disposable that will unregister the callback when disposed.</returns>
		internal IDisposable RegisterCompiledBindingsUpdateCallback(Action handler)
		{
			var weakDelegate = CreateWeakDelegate(handler);

			_compiledBindingsCallbacks = _compiledBindingsCallbacks.Add(weakDelegate.callback);

			// This weak reference ensure that the closure will not link
			// the caller and the callee, in the same way "newValueActionWeak"
			// does not link the callee to the caller.
			var instanceRef = _thisWeakRef;

			return new DispatcherConditionalDisposable(
				handler.Target,
				instanceRef.CloneWeakReference(),
				() =>
				{
					var that = instanceRef.Target as DependencyObjectStore;

					if (that != null)
					{
						// Delegates integrate a null check when removing new delegates.
						that._compiledBindingsCallbacks = that._compiledBindingsCallbacks.Remove(weakDelegate.callback);
					}

					weakDelegate.release.Dispose();

					// Force a closure on the callback, to make its lifetime as long
					// as the subscription being held by the callee.
					handler = null;
				}
			);
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
			var instanceRef = _thisWeakRef;

			return new DispatcherConditionalDisposable(
				callback.Target,
				instanceRef.CloneWeakReference(),
				() =>
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
					callback = null;
				}
			);
		}

		internal (object value, DependencyPropertyValuePrecedences precedence) GetValueUnderPrecedence(DependencyProperty property, DependencyPropertyValuePrecedences precedence)
		{
			var stack = _properties.GetPropertyDetails(property);

			return stack.GetValueUnderPrecedence(precedence);
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

		private void TryRegisterInheritedProperties(IDependencyObjectStoreProvider parentProvider = null, bool force = false)
		{
			if (
				!_registeringInheritedProperties
				&& !_unregisteringInheritedProperties
				&& _inheritedProperties.Disposable == null
				&& (
					IsAutoPropertyInheritanceEnabled
					|| force

					// these two cases may be required in case the
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
					// See https://github.com/mono/mono/issues/13653 for more details.
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

		internal IDisposable RegisterInheritedProperties(IDependencyObjectStoreProvider parentProvider)
		{

			// Initialize at two as there is at most two disposables added below, and
			// there is no need to allocate for more internally.
			var disposable = new CompositeDisposable(2);

			_parentTemplatedParentProperty = parentProvider.Store.TemplatedParentProperty;
			_parentDataContextProperty = parentProvider.Store.DataContextProperty;

			// The propagation of the inherited properties is performed by setting the
			// Inherited precedence level value of each control of the visual tree.
			// This is performed in three ways:
			//    - By listening to the parent's property changes,
			//    - By replicating the parent's current state when a DependencyObject parent's is set
			//    - By forcing a property update notification down to a DependencyObject's children when the parent is set.

			// Subscribe to the parent's notifications
			parentProvider
				.Store
				.RegisterInheritedPropertyChangedCallback(this)
				.DisposeWith(disposable);

			// Force propagation for inherited properties defined on the current instance.
			PropagateInheritedProperties();

			// Register for unset values
			disposable.Add(() =>
			{
#if !HAS_EXPENSIVE_TRYFINALLY
				// The try/finally incurs a very large performance hit in mono-wasm, and SetValue is in a very hot execution path.
				// See https://github.com/mono/mono/issues/13653 for more details.
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

						SetValue(_dataContextProperty, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Inheritance);
						SetValue(_templatedParentProperty, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Inheritance);
					}
				}
#if !HAS_EXPENSIVE_TRYFINALLY
				finally
#endif
				{
					_unregisteringInheritedProperties = false;
				}
			});

			return disposable;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private (DependencyProperty localProperty, DependencyPropertyDetails propertyDetails) GetLocalPropertyDetails(DependencyProperty property)
		{
			if (_parentDataContextProperty.UniqueId == property.UniqueId)
			{
				return (_dataContextProperty, _dataContextPropertyDetails);
			}
			else if (_parentTemplatedParentProperty == property)
			{
				return (_templatedParentProperty, _templatedParentPropertyDetails);
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

		private void InvokeCompiledBindingsCallbacks()
		{
			for (var compiledBindingsCBIndex = 0; compiledBindingsCBIndex < _compiledBindingsCallbacks.Data.Length; compiledBindingsCBIndex++)
			{
				var callback = _compiledBindingsCallbacks.Data[compiledBindingsCBIndex];
				callback.Invoke();
			}
		}

		/// <summary>
		/// Propagate the current inheritable properties to the registered children.
		/// </summary>
		internal void PropagateInheritedProperties(DependencyObjectStore childStore = null)
		{
			// Raise the property change for the current values
			var props = DependencyProperty.GetFrameworkPropertiesForType(_originalObjectType, FrameworkPropertyMetadataOptions.Inherits);

			// Not using the ActualInstance property here because we need to get a WeakReference instead.
			var instanceRef = _originalObjectRef != null ? _originalObjectRef : _thisWeakRef;

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

		private void PropagateInheritedNonLocalProperties(DependencyObjectStore childStore)
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

		private static bool IsAncestor(DependencyObject instance, Dictionary<object, bool> map, object ancestor)
		{
#if DEBUG
			var hashSet = new HashSet<DependencyObject>(Uno.ReferenceEqualityComparer<DependencyObject>.Default);
			hashSet.Add(instance);
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
					if (!hashSet.Contains(instance))
					{
						// Console.WriteLine($"Added other {(instance as FrameworkElement)?.Name}");
						hashSet.Add(instance);
					}
					else
					{
						throw new Exception($"Cycle detected: [{prevInstance}/{(prevInstance as FrameworkElement)?.Name}] has already added [{instance}/{(instance as FrameworkElement).Name}] as parent/");
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

		public DependencyObject ActualInstance
			=> _originalObjectRef.Target as DependencyObject;

		/// <summary>
		/// Finds the first dependency object that matches the specified <paramref name="ownerType"/>, self included.
		/// </summary>
		/// <param name="instance">The instance used to walk up the tree</param>
		/// <param name="ownerType">The owner type to find</param>
		/// <returns>A known parent instance, otherwise null.</returns>
		private Tuple<DependencyObject, DependencyProperty> FindFirstInheritanceParent(DependencyObject instance, string name)
		{
			do
			{
				var property = DependencyProperty.GetProperty(instance.GetType(), name);

				if (property != null)
				{
					return Tuple.Create(instance, property);
				}

				instance = DependencyObjectExtensions.GetParent(instance) as DependencyObject;
			}
			while (instance != null);

			return null;
		}

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
				(s, e) => (wr.Target as PropertyChangedCallback)?.Invoke(s, e);

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
			object previousValue,
			DependencyPropertyValuePrecedences previousPrecedence,
			object newValue,
			DependencyPropertyValuePrecedences newPrecedence
		)
		{
			var hasPropagationBypass = _propagationBypass.Count != 0 || _propagationBypassed.Count != 0;

			// This check is present to avoid allocating if there is no bypass.
			var propertyPath = hasPropagationBypass ? new DependencyPropertyPath(actualInstanceAlias, propertyDetails.Property) : null;

			if (AreDifferent(newValue, previousValue))
			{
				var bypassesPropagation = hasPropagationBypass && _propagationBypass.Contains(propertyPath);

				if (bypassesPropagation)
				{
					_propagationBypassed[propertyPath] = previousValue;
				}

				InvokeCallbacks(actualInstanceAlias, propertyDetails.Property, propertyDetails, previousValue, previousPrecedence, newValue, newPrecedence, bypassesPropagation);
			}
			else if (
				hasPropagationBypass
				&& _propagationBypassed.ContainsKey(propertyPath)
				&& !_propagationBypass.Contains(propertyPath, DependencyPropertyPath.Comparer.Default)
			)
			{
				// If unchanged, but previous value was set with propagation bypass enabled (and we are currently being set without bypass enabled),
				// then we should invoke callbacks so that the value can be propagated. This arises in animation scenarios.
				var unpropagatedPrevious = _propagationBypassed[propertyPath];
				_propagationBypassed.Remove(propertyPath);

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
			object previousValue,
			DependencyPropertyValuePrecedences previousPrecedence,
			object newValue,
			DependencyPropertyValuePrecedences newPrecedence,
			bool bypassesPropagation = false
		)
		{
			var eventArgs = new DependencyPropertyChangedEventArgs(property, previousValue, previousPrecedence, newValue, newPrecedence, bypassesPropagation);
			var propertyMetadata = propertyDetails.Metadata;

			// We can reuse the weak reference, otherwise capture the weak reference to this instance.
			var instanceRef = _originalObjectRef ?? _thisWeakRef;

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
						var store = _childrenStores[storeIndex];
						store.OnParentPropertyChangedCallback(instanceRef, property, eventArgs);
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

			// Raise the changes for the callback register to the property itself
			propertyMetadata.RaisePropertyChanged(actualInstanceAlias, eventArgs);

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

		/// <summary>
		/// Updates the parent of the <paramref name="newValue"/> to the
		/// <paramref name="actualInstanceAlias"/> and resets the parent of <paramref name="previousValue"/>.
		/// </summary>
		private static void UpdateAutoParent(DependencyObject actualInstanceAlias, object previousValue, object newValue)
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
			object value,
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
		public static bool AreDifferent(object previousValue, object newValue)
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

		private void OnParentChanged(object previousParent, object value)
		{
			if (_parentChangedCallbacks.Data.Length != 0)
			{
				var actualInstanceAlias = ActualInstance;

				var args = new DependencyObjectParentChangedEventArgs(previousParent, value);

				for (var parentCallbackIndex = 0; parentCallbackIndex < _parentChangedCallbacks.Data.Length; parentCallbackIndex++)
				{
					var handler = _parentChangedCallbacks.Data[parentCallbackIndex];
					handler.Invoke(actualInstanceAlias, null, args);
				}
			}
		}

		private class DependencyPropertyPath : IEquatable<DependencyPropertyPath>
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

			public bool Equals(DependencyPropertyPath other)
				=> other != null &&
					ReferenceEquals(this.Instance, other.Instance) &&
					this.Property.UniqueId == other.Property.UniqueId;

			public class Comparer : IEqualityComparer<DependencyPropertyPath>
			{
				public static Comparer Default { get; } = new Comparer();

				private Comparer()
				{
				}

				public bool Equals(DependencyPropertyPath x, DependencyPropertyPath y)
					=> x.Equals(y);

				public int GetHashCode(DependencyPropertyPath obj)
					=> obj.GetHashCode();
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
