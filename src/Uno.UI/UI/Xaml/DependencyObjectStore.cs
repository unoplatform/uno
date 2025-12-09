#nullable enable

using System;
using Uno.UI.DataBinding;
using System.Collections.Generic;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Diagnostics.Eventing;
using Uno.Disposables;
using System.Linq;
using System.Threading;
using Uno.Collections;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Microsoft.UI.Xaml.Data;
using Uno.UI;
using System.Collections;
using System.Globalization;
using Windows.ApplicationModel.Calls;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Media;

#if __ANDROID__
using View = Android.Views.View;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
#endif

namespace Microsoft.UI.Xaml
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

		private bool _isDisposed;

		private readonly DependencyPropertyDetailsCollection _properties;
		private ResourceBindingCollection? _resourceBindings;

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
		private InheritedPropertiesDisposable? _inheritedProperties;
		private ManagedWeakReference? _parentRef;
		private object? _hardParentRef;
		private readonly Dictionary<DependencyProperty, ManagedWeakReference> _inheritedForwardedProperties = new Dictionary<DependencyProperty, ManagedWeakReference>(DependencyPropertyComparer.Default);
		private Stack<DependencyPropertyValuePrecedences?>? _overriddenPrecedences;

		private static long _propertyChangedToken;
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
		private bool _isSettingPersistentResourceBinding;
		/// <summary>
		/// The theme last to apply theme bindings on this object and its children.
		/// </summary>
		private SpecializedResourceDictionary.ResourceKey? _themeLastUsed;

		internal bool IsDisposed => _isDisposed;

		private InheritedPropertiesDisposable? InheritedProperties
		{
			get => _inheritedProperties;
			set
			{
				_inheritedProperties?.Dispose();
				_inheritedProperties = value;
			}
		}

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

					InheritedProperties = null;

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
		/// This is used by HotReload so that type replacement doesn't cause property values to be lost.
		/// </summary>
		internal void ClonePropertiesToAnotherStoreForHotReload(DependencyObjectStore otherStore)
		{
			_properties.CloneToForHotReload(otherStore._properties, this, otherStore);
		}

		/// <summary>
		/// Creates a delegated dependency object instance for the specified <paramref name="originalObject"/>
		/// </summary>
		/// <param name="originalObject"></param>
		[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "normal flow of operation")]
		public DependencyObjectStore(object originalObject, DependencyProperty dataContextProperty)
		{
			_originalObjectRef = WeakReferencePool.RentWeakReference(this, originalObject);
			_originalObjectType = originalObject is AttachedDependencyObject a ? a.Owner.GetType() : originalObject.GetType();

			_properties = new DependencyPropertyDetailsCollection(_originalObjectRef, dataContextProperty);

			_dataContextProperty = dataContextProperty;

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
			TemplatedParentScope.UpdateTemplatedParentIfNeeded(originalObject as DependencyObject, store: this);
#endif

			if (_trace.IsEnabled)
			{
				_trace.WriteEvent(
					DependencyObjectStore.TraceProvider.CreationTask,
					new object[] { GetHashCode(), _originalObjectType.Name }
				);
			}
		}

		public DependencyObjectStore(object originalObject, DependencyProperty dataContextProperty, DependencyProperty templatedParentProperty)
			: this(originalObject, dataContextProperty)
		{
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
		/// properties such as <see cref="DataContextProperty"/>.
		/// </summary>
		/// <remarks>
		/// This is used to avoid propagating the DataContext property
		/// for types that commonly do not expose inherited properties, such as visual states.
		/// </remarks>
		public bool IsAutoPropertyInheritanceEnabled { get; set; } = true;

		/// <summary>
		/// Returns the current effective value of a dependency property from a DependencyObject.
		/// </summary>
		/// <param name="property">The <see cref="DependencyProperty" /> identifier of the property for which to retrieve the value. </param>
		/// <returns>Returns the current effective value.</returns>
		public object? GetValue(DependencyProperty property)
		{
			WritePropertyEventTrace(TraceProvider.GetValue, property, null);

			ValidatePropertyOwner(property);

			if (_properties.DataContextPropertyDetails.Property == property)
			{
				// Historically, we didn't have this fast path for default value.
				// We add this to maintain the original behavior in GetValue(DependencyPropertyDetails) overload.
				// This should be revisited in future.
				TryRegisterInheritedProperties(force: true);
			}

			DependencyPropertyDetails? propertyDetails = _properties.FindPropertyDetails(property);
			if (propertyDetails is null)
			{
				// Performance: Avoid force-creating DependencyPropertyDetails when not needed.
				return GetDefaultValue(property);
			}

			return propertyDetails.CurrentHighestValuePrecedence == DependencyPropertyValuePrecedences.DefaultValue
				? GetDefaultValue(propertyDetails.Property)
				: propertyDetails.GetEffectiveValue();
		}

		/// <summary>
		/// Returns the local value of a dependency property, if a local value is set.
		/// </summary>
		/// <param name="property">The dependency property to get</param>
		/// <returns></returns>
		public object? ReadLocalValue(DependencyProperty property)
		{
			var details = _properties.FindPropertyDetails(property);
			if (property == _dataContextProperty)
			{
				TryRegisterInheritedProperties(force: true);
			}

			if (details?.GetBaseValueSource() == DependencyPropertyValuePrecedences.Local)
			{
				return details.GetBaseValue();
			}

			return DependencyProperty.UnsetValue;
		}

		public object? ReadInheritedValueOrDefaultValue(DependencyProperty property)
		{
			var details = _properties.FindPropertyDetails(property);
			if (property == _dataContextProperty)
			{
				TryRegisterInheritedProperties(force: true);
			}

			if (details is null)
			{
				return GetDefaultValue(property);
			}

			var inheritedValue = details.GetInheritedValue();
			return inheritedValue == DependencyProperty.UnsetValue ? GetDefaultValue(property) : inheritedValue;
		}

		/// <summary>
		/// Returns the local value of a dependency property, if a local value is set.
		/// </summary>
		/// <param name="property">The dependency property to get</param>
		/// <returns></returns>
		public object? GetAnimationBaseValue(DependencyProperty property)
		{
			var (modifiedValue, details) = GetModifiedValue(property);
			if (modifiedValue?.IsAnimated == true)
			{
				if (property == _dataContextProperty)
				{
					TryRegisterInheritedProperties(force: true);
				}

				if (details!.GetBaseValueSource() == DependencyPropertyValuePrecedences.DefaultValue)
				{
					return GetDefaultValue(property);
				}

				return modifiedValue.GetBaseValue();
			}

			return GetValue(property);
		}

		private (ModifiedValue? modifiedValue, DependencyPropertyDetails? details) GetModifiedValue(DependencyProperty property)
		{
			var details = _properties.FindPropertyDetails(property);
			if (details is null)
			{
				return (null, null);
			}

			return (details.GetModifiedValue(), details);
		}

		private object? GetValue(DependencyPropertyDetails propertyDetails)
		{
			if (propertyDetails == _properties.DataContextPropertyDetails)
			{
				TryRegisterInheritedProperties(force: true);
			}

			return propertyDetails.CurrentHighestValuePrecedence == DependencyPropertyValuePrecedences.DefaultValue
				? GetDefaultValue(propertyDetails.Property)
				: propertyDetails.GetEffectiveValue();
		}

		/// <summary>
		/// Determines the current highest dependency property value precedence
		/// </summary>
		/// <param name="propertyDetails">The dependency property to get</param>
		/// <returns></returns>
		internal DependencyPropertyValuePrecedences GetCurrentHighestValuePrecedence(DependencyPropertyDetails propertyDetails)
		{
			return propertyDetails.CurrentHighestValuePrecedence;
		}

		/// <summary>
		/// Determines the current highest dependency property value precedence
		/// </summary>
		/// <param name="property">The dependency property to get</param>
		/// <returns></returns>
		internal DependencyPropertyValuePrecedences GetCurrentHighestValuePrecedence(DependencyProperty property)
		{
			// There is no need to force-create property details here.
			// If it's not yet created, then simply the precedence is DefaultValue
			return _properties.FindPropertyDetails(property)?.CurrentHighestValuePrecedence ?? DependencyPropertyValuePrecedences.DefaultValue;
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

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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

				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
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
		/// <param name="property">The dependency property to get</param>
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

		internal void SetValue(DependencyProperty property, object? value, DependencyPropertyValuePrecedences precedence, DependencyPropertyDetails? propertyDetails = null, bool isPersistentResourceBinding = false)
		{
			if (_trace.IsEnabled)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void SetValueWithTrace(DependencyProperty property, object? value, DependencyPropertyValuePrecedences precedence, DependencyPropertyDetails? propertyDetails, bool isPersistentResourceBinding)
				{
					using (WritePropertyEventTrace(TraceProvider.SetValueStart, TraceProvider.SetValueStop, property, precedence))
					{
						InnerSetValue(property, value, precedence, propertyDetails, isPersistentResourceBinding);
					}
				}

				SetValueWithTrace(property, value, precedence, propertyDetails, isPersistentResourceBinding);
			}
			else
			{
				InnerSetValue(property, value, precedence, propertyDetails, isPersistentResourceBinding);
			}
		}

		private void InnerSetValue(DependencyProperty property, object? value, DependencyPropertyValuePrecedences precedence, DependencyPropertyDetails? propertyDetails, bool isPersistentResourceBinding)
		{
			if (precedence == DependencyPropertyValuePrecedences.Coercion)
			{
				throw new ArgumentException("SetValue must not be called with precedence DependencyPropertyValuePrecedences.Coercion, as it expects a non-coerced value to function properly.");
			}

			var actualInstanceAlias = ActualInstance;

			if (actualInstanceAlias != null)
			{
				var overrideDisposable = ApplyPrecedenceOverride(ref precedence);

				try
				{
					if ((value == DependencyProperty.UnsetValue) && precedence == DependencyPropertyValuePrecedences.DefaultValue)
					{
						throw new InvalidOperationException("The default value must be a valid value");
					}

					ValidatePropertyOwner(property);

					// Resolve the stack once for the instance, for performance.
					propertyDetails ??= _properties.GetPropertyDetails(property);

					if (precedence <= DependencyPropertyValuePrecedences.Local)
					{
						TryClearBinding(value, propertyDetails);
					}

					var previousValue = GetValue(propertyDetails);
					var previousPrecedence = GetCurrentHighestValuePrecedence(propertyDetails);

					// Coercion must be applied before we set the new value
					// see https://github.com/unoplatform/uno/pull/12884
					ApplyCoercion(actualInstanceAlias, propertyDetails, previousValue, value, precedence);

					SetValueInternal(value, precedence, propertyDetails);

					if (!isPersistentResourceBinding && !_isSettingPersistentResourceBinding)
					{
						// If a non-theme value is being set, clear any theme binding so it's not overwritten if the theme changes.
						_resourceBindings?.ClearBinding(property, precedence);
					}

					// Value may or may not have changed based on the precedence
					var newValue = GetValue(propertyDetails);
					var newPrecedence = GetCurrentHighestValuePrecedence(propertyDetails);

					if (property == _dataContextProperty)
					{
						OnDataContextChanged(value, newValue, precedence);
					}

					TryApplyDataContextOnPrecedenceChange(property, propertyDetails, previousValue, previousPrecedence, newValue, newPrecedence);

					TryUpdateInheritedAttachedProperty(property, propertyDetails);

					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						var name = (_originalObjectRef.Target as IFrameworkElement)?.Name ?? _originalObjectRef.Target?.GetType().Name;
						var hashCode = _originalObjectRef.Target?.GetHashCode();

						this.Log().Debug(
							$"SetValue on [{name}/{hashCode:X8}] for [{property.Name}] to [{newValue}] (req:{value} reqp:{precedence} p:{previousValue} pp:{previousPrecedence} np:{newPrecedence})"
						);
					}

					RaiseCallbacks(actualInstanceAlias, propertyDetails, previousValue, previousPrecedence, newValue, newPrecedence);
				}
				finally
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

		/// <summary>
		/// Tries to apply the DataContext to the new and previous values when DataContext Value is inherited
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void TryApplyDataContextOnPrecedenceChange(
			DependencyProperty property,
			DependencyPropertyDetails propertyDetails,
			object? previousValue,
			DependencyPropertyValuePrecedences previousPrecedence,
			object? newValue,
			DependencyPropertyValuePrecedences newPrecedence)
		{
			if (property.IsUnoType
				&& propertyDetails.HasValueInherits
				&& !propertyDetails.HasValueDoesNotInherit)
			{
				// This block is used to synchronize the DataContext property of DependencyProperty values marked as
				// being able to inherit the DataContext.
				// When a DependencyProperty has a bindable default value instance (e.g. ContentControl.Foreground), and that the
				// ContentControl has a DataContext, setting a local precedence value should clear the default value instance DataContext
				// property. This avoid inaccessible DataContext instances to be strongly kept alive by a lower precedence that
				// may never be used again.

				// If a value is set with a higher precedence, the lower precedence DataContext must be cleared
				if (newPrecedence < previousPrecedence)
				{
					if (previousValue is IDependencyObjectStoreProvider childProviderClear)
					{
						// Clears the DataContext of the previous precedence value
						childProviderClear.Store.ClearInheritedDataContext();
					}

					if (newValue is IDependencyObjectStoreProvider childProviderClearNewValue
						&& !ReferenceEquals(childProviderClearNewValue.Store.Parent, ActualInstance))
					{
						// Sets the DataContext of the new precedence value
						childProviderClearNewValue.Store.RestoreInheritedDataContext(GetHighestValueFromDetails(_properties.DataContextPropertyDetails));
					}
				}

				// If a value is set with a lower precedence, the higher precedence DataContext must be set to the current DataContext
				if (newPrecedence > previousPrecedence)
				{
					if (newValue is IDependencyObjectStoreProvider childProviderSet
						&& !ReferenceEquals(childProviderSet.Store.Parent, ActualInstance))
					{
						// Sets the DataContext of the new precedence value
						childProviderSet.Store.RestoreInheritedDataContext(GetHighestValueFromDetails(_properties.DataContextPropertyDetails));
					}

					if (previousValue is IDependencyObjectStoreProvider childProviderSetNewValue)
					{
						// Clears the DataContext of the previous precedence value
						childProviderSetNewValue.Store.ClearInheritedDataContext();
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void ClearInheritedDataContext()
		{
			ClearValue(_dataContextProperty, DependencyPropertyValuePrecedences.Inheritance);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RestoreInheritedDataContext(object? dataContext)
		{
			SetValue(_dataContextProperty, dataContext, DependencyPropertyValuePrecedences.Inheritance);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void TryClearBinding(object? value, DependencyPropertyDetails propertyDetails)
		{
			if (object.ReferenceEquals(value, DependencyProperty.UnsetValue))
			{
				var hasTemplatedParentBinding =
					propertyDetails.GetBinding()?.ParentBinding.RelativeSource?.Mode == RelativeSourceMode.TemplatedParent;

				if (!hasTemplatedParentBinding)
				{
					propertyDetails.ClearBinding();
				}
			}
		}

		private void TryUpdateInheritedAttachedProperty(DependencyProperty property, DependencyPropertyDetails propertyDetails)
		{
			if (property.IsAttached && property.IsInherited)
			{
				// Add inheritable attached properties to the inherited forwarded
				// properties, so they can be automatically propagated when a child
				// store is late added.
				_inheritedForwardedProperties[property] = _originalObjectRef;
			}
		}

		private void ApplyCoercion(DependencyObject actualInstanceAlias, DependencyPropertyDetails propertyDetails,
			object? previousValue, object? baseValue, DependencyPropertyValuePrecedences precedence)
		{
			if (baseValue == DependencyProperty.UnsetValue)
			{
				// Removing any previously applied coercion
				SetValueInternal(DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Coercion, propertyDetails);

				// CoerceValueCallback shouldn't be called when unsetting the value.
				return;
			}

			var coerceValueCallback = propertyDetails.Property.Metadata.CoerceValueCallback;
			if (coerceValueCallback == null)
			{
				// No coercion to remove or to apply.
				return;
			}

			var options = (propertyDetails.Property.Metadata as FrameworkPropertyMetadata)?.Options ?? FrameworkPropertyMetadataOptions.Default;

			if (Equals(previousValue, baseValue) && ((options & FrameworkPropertyMetadataOptions.CoerceOnlyWhenChanged) != 0))
			{
				// Value hasn't changed, don't coerce.
				return;
			}

			var coercedValue = coerceValueCallback(actualInstanceAlias, baseValue, precedence);
			if (coercedValue == DependencyProperty.UnsetValue)
			{
				// The property system will treat any CoerceValueCallback that returns the value UnsetValue as a special case.
				// This special case means that the property change that resulted in the CoerceValueCallback being called
				// should be rejected by the property system, and that the property system should instead report whatever
				// previous value the property had.
				// Source: https://msdn.microsoft.com/en-us/library/ms745795%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
				SetValueInternal(previousValue, DependencyPropertyValuePrecedences.Coercion, propertyDetails);
			}
			else if (!Equals(coercedValue, baseValue) || ((options & FrameworkPropertyMetadataOptions.KeepCoercedWhenEquals) != 0))
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
			var (baseValue, basePrecedence) = GetBaseValue(property);
			if (basePrecedence == DependencyPropertyValuePrecedences.DefaultValue && property.IsInherited)
			{
				basePrecedence = DependencyPropertyValuePrecedences.Inheritance;
			}

			SetValue(property, baseValue, basePrecedence);
		}

		private void WritePropertyEventTrace(int eventId, DependencyProperty property, DependencyPropertyValuePrecedences? precedence)
		{
			if (_trace.IsEnabled)
			{
				_trace.WriteEvent(eventId, new object[] { GetHashCode(), property.OwnerType.Name, property.Name, precedence?.ToString() ?? "Local" });
			}
		}

		private IDisposable? WritePropertyEventTrace(int startEventId, int stopEventId, DependencyProperty property, DependencyPropertyValuePrecedences precedence)
		{
			if (_trace.IsEnabled)
			{
				return _trace.WriteEventActivity(
					startEventId,
					stopEventId,
					new object[] { GetHashCode(), property.OwnerType.Name, property.Name, precedence.ToString() }
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
			if (FeatureConfiguration.DependencyProperty.ValidatePropertyOwnerOnReadWrite)
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
			propertyDetails ??= _properties.GetPropertyDetails(property);

			if (ReferenceEquals(callback.Target, ActualInstance))
			{
				return propertyDetails.RegisterCallback(callback);
			}
			else
			{
				CreateWeakDelegate(callback, out var weakCallback, out var weakDelegateRelease);

				var cookie = propertyDetails.RegisterCallback(weakCallback);

				return new RegisterPropertyChangedCallbackForPropertyConditionalDisposable(
					callback,
					weakDelegateRelease,
					cookie,
					ThisWeakReference
				);
			}
		}

		/// <summary>
		/// Specialized <see cref="DispatcherConditionalDisposable"/> for
		/// <see cref="RegisterPropertyChangedCallback(DependencyProperty, PropertyChangedCallback, DependencyPropertyDetails?)"/>.
		/// </summary>
		/// <remarks>
		/// This class is used to avoid the creation of a set of <see cref="Action"/> instances, as well as delegate invocations.
		/// </remarks>
		private class RegisterPropertyChangedCallbackForPropertyConditionalDisposable : DispatcherConditionalDisposable
		{
			private PropertyChangedCallback _callback;
			private WeakReferenceReturnDisposable _releaseWeakDelegate;
			private IDisposable _callbackManagedCookie;
			private ManagedWeakReference _doStoreRef;

			public RegisterPropertyChangedCallbackForPropertyConditionalDisposable(
				PropertyChangedCallback callback,
				WeakReferenceReturnDisposable releaseWeakDelegate,
				IDisposable callbackManagerCookie,
				ManagedWeakReference doStoreRef)
				: base(callback.Target, doStoreRef.CloneWeakReference())
			{
				_callback = callback;
				_releaseWeakDelegate = releaseWeakDelegate;
				_callbackManagedCookie = callbackManagerCookie;
				_doStoreRef = doStoreRef;
			}

			protected override void DispatchedTargetFinalized()
			{
				// This weak reference ensure that the closure will not link
				// the caller and the callee, in the same way "newValueActionWeak"
				// does not link the callee to the caller.
				if (_doStoreRef.Target is DependencyObjectStore that)
				{
					_callbackManagedCookie.Dispose();
					_releaseWeakDelegate.Dispose();

					// Force a closure on the callback, to make its lifetime as long
					// as the subscription being held by the callee.
					_callback = null!;
				}
			}
		}

		internal IDisposable RegisterPropertyChangedCallback(ExplicitPropertyChangedCallback handler)
		{
			if (ReferenceEquals(handler.Target, ActualInstance))
			{
				_genericCallbacks = _genericCallbacks.Add(handler);

				return Disposable.Create(() => _genericCallbacks = _genericCallbacks.Remove(handler));
			}
			else
			{
				CreateWeakDelegate(handler, out var weakHandler, out var weakHandlerRelease);

				// Delegates integrate a null check when adding new delegates.
				_genericCallbacks = _genericCallbacks.Add(weakHandler);

				return new RegisterPropertyChangedCallbackConditionalDisposable(
					weakHandler,
					weakHandlerRelease,
					ThisWeakReference,
					handler
				);
			}
		}

		/// <summary>
		/// Specialized DispatcherConditionalDisposable for <see cref="RegisterPropertyChangedCallback(ExplicitPropertyChangedCallback)"/>
		/// </summary>
		/// <remarks>
		/// This class is used to avoid the creation of a set of <see cref="Action"/> instances, as well as delegate invocations.
		/// </remarks>
		private class RegisterPropertyChangedCallbackConditionalDisposable : DispatcherConditionalDisposable
		{
			private ExplicitPropertyChangedCallback _weakCallback;
			private WeakReferenceReturnDisposable _weakCallbackRelease;
			private ManagedWeakReference _instanceRef;
			private ExplicitPropertyChangedCallback _callback;

			public RegisterPropertyChangedCallbackConditionalDisposable(
				ExplicitPropertyChangedCallback weakCallback,
				WeakReferenceReturnDisposable weakCallbackRelease,
				ManagedWeakReference instanceRef,
				ExplicitPropertyChangedCallback callback)
				: base(callback.Target, instanceRef.CloneWeakReference())
			{
				_weakCallback = weakCallback;
				_weakCallbackRelease = weakCallbackRelease;
				_instanceRef = instanceRef;
				_callback = callback;
			}

			protected override void DispatchedTargetFinalized()
			{
				// This weak reference ensure that the closure will not link
				// the caller and the callee, in the same way "newValueActionWeak"
				// does not link the callee to the caller.
				if (_instanceRef.Target is DependencyObjectStore that)
				{
					// Delegates integrate a null check when removing new delegates.
					that._genericCallbacks = that._genericCallbacks.Remove(_weakCallback);
				}

				_weakCallbackRelease.Dispose();

				// Force a closure on the callback, to make its lifetime as long
				// as the subscription being held by the callee.
				_callback = null!;
			}
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
			if (ReferenceEquals(callback.Target, ActualInstance))
			{
				_parentChangedCallbacks = _parentChangedCallbacks.Add(callback);

				return Disposable.Create(() => _parentChangedCallbacks = _parentChangedCallbacks.Remove(callback));
			}
			else
			{
				var weakCallbackRef = WeakReferencePool.RentWeakReference(this, callback);

				ParentChangedCallback weakCallback =
					(s, _, e) => (!weakCallbackRef.IsDisposed ? weakCallbackRef.Target as ParentChangedCallback : null)?.Invoke(s, key, e);

				_parentChangedCallbacks = _parentChangedCallbacks.Add(weakCallback);

				// This weak reference ensure that the closure will not link
				// the caller and the callee, in the same way "newValueActionWeak"
				// does not link the callee to the caller.
				var instanceRef = ThisWeakReference;

				return new RegisterParentChangedCallbackConditionalDisposable(
					instanceRef.CloneWeakReference(),
					instanceRef,
					weakCallbackRef,
					weakCallback,
					callback
				);
			}
		}

		/// <summary>
		/// Specialized DispatcherConditionalDisposable for <see cref="RegisterParentChangedCallback(object, ParentChangedCallback)"/>
		/// </summary>
		/// <remarks>
		/// This class is used to avoid the creation of a set of <see cref="Action"/> instances, as well as delegate invocations.
		/// </remarks>
		private class RegisterParentChangedCallbackConditionalDisposable : DispatcherConditionalDisposable
		{
			private ManagedWeakReference _doStoreRef;
			private ManagedWeakReference _weakCallbackRef;
			private ParentChangedCallback _weakCallback;
			private ParentChangedCallback _callback;

			public RegisterParentChangedCallbackConditionalDisposable(
				WeakReference conditionSource,
				ManagedWeakReference doStoreRef,
				ManagedWeakReference weakCallbackRef,
				ParentChangedCallback weakCallback,
				ParentChangedCallback callback) : base(callback.Target, conditionSource)
			{
				_doStoreRef = doStoreRef;
				_weakCallbackRef = weakCallbackRef;
				_weakCallback = weakCallback;
				_callback = callback;
			}

			protected override void DispatchedTargetFinalized()
			{
				var that = _doStoreRef.Target as DependencyObjectStore;

				if (that != null)
				{
					// Delegates integrate a null check when removing new delegates.
					that._parentChangedCallbacks = that._parentChangedCallbacks.Remove(_weakCallback);
				}

				WeakReferencePool.ReturnWeakReference(that, _weakCallbackRef);

				// Force a closure on the callback, to make its lifetime as long
				// as the subscription being held by the callee.
				_callback = null!;
			}
		}

		/// <summary>
		/// Registers to parent changes for self
		/// </summary>
		/// <param name="key">A key to be passed to the callback parameter.</param>
		/// <param name="callback">A callback to be called</param>
		internal void RegisterParentChangedCallbackStrong(object key, ParentChangedCallback callback)
			=> _parentChangedCallbacks = _parentChangedCallbacks.Add((s, _, e) => callback(s, key, e));

		internal (object? value, DependencyPropertyValuePrecedences precedence) GetBaseValue(DependencyProperty property)
		{
			var details = _properties.FindPropertyDetails(property);
			if (property == _dataContextProperty)
			{
				TryRegisterInheritedProperties(force: true);
			}

			if (details is null)
			{
				return (GetDefaultValue(property), DependencyPropertyValuePrecedences.DefaultValue);
			}

			var baseValueSource = details.GetBaseValueSource();
			if (baseValueSource == DependencyPropertyValuePrecedences.DefaultValue)
			{
				return (GetDefaultValue(property), DependencyPropertyValuePrecedences.DefaultValue);
			}

			return (details.GetBaseValue(), baseValueSource);
		}

		internal object? GetAnimatedValue(DependencyProperty property)
		{
			var (modifiedValue, _) = GetModifiedValue(property);
			if (modifiedValue is not null)
			{
				return modifiedValue.GetAnimatedValue();
			}

			return DependencyProperty.UnsetValue;
		}

		// Internal for unit testing
		internal object GetDefaultValue(DependencyProperty dp)
		{
			var actualInstance = ActualInstance;
			var defaultValue = dp.GetDefaultValue(actualInstance, actualInstance?.GetType());
			if (GetCurrentHighestValuePrecedence(dp) == DependencyPropertyValuePrecedences.DefaultValue &&
				// This should be for OnDemand DPs in general which we don't yet support
				dp == UIElement.KeyboardAcceleratorsProperty)
			{
				_properties.GetPropertyDetails(dp).SetValue(defaultValue, DependencyPropertyValuePrecedences.Local);
			}

			return defaultValue;
		}

		private object? GetHighestValueFromDetails(DependencyPropertyDetails details)
			=> details.CurrentHighestValuePrecedence == DependencyPropertyValuePrecedences.DefaultValue
				? GetDefaultValue(details.Property)
				: details.GetEffectiveValue();

		internal DependencyPropertyDetails GetPropertyDetails(DependencyProperty property)
		{
			return _properties.GetPropertyDetails(property);
		}

		// Keep a list of inherited properties that have been updated so they can be reset.
		HashSet<DependencyProperty>? _updatedProperties;

		private void OnParentPropertyChangedCallback(ManagedWeakReference sourceInstance, DependencyProperty parentProperty, object? newValue)
		{
			var (localProperty, propertyDetails) = GetLocalPropertyDetails(parentProperty);

			if (localProperty != null)
			{
				// If the property is available on the current DependencyObject, update it.
				// This will allow for it to be reset to is previous lower precedence.
				if (localProperty != _dataContextProperty &&
					(_updatedProperties is null || !_updatedProperties.Contains(localProperty))
				)
				{
					(_updatedProperties ??= new HashSet<DependencyProperty>(DependencyPropertyComparer.Default)).Add(localProperty);
				}

				SetValue(localProperty, newValue, DependencyPropertyValuePrecedences.Inheritance, propertyDetails);
			}
			else
			{
				// Always update the inherited properties with the new value, the instance
				// may change if a far ancestor changed.
				_inheritedForwardedProperties[parentProperty] = sourceInstance;

				// If not, propagate the DP down to the child listeners, if any.
				var localChildrenStores = _childrenStores;
				for (var storeIndex = 0; storeIndex < localChildrenStores.Count; storeIndex++)
				{
					var child = localChildrenStores[storeIndex];
					child.OnParentPropertyChangedCallback(sourceInstance, parentProperty, newValue);
				}
			}
		}

		private void TryRegisterInheritedProperties(IDependencyObjectStoreProvider? parentProvider = null, bool force = false)
		{
			if (
				!_registeringInheritedProperties
				&& !_unregisteringInheritedProperties
				&& InheritedProperties == null
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
					try
					{
						_registeringInheritedProperties = true;

						InheritedProperties = RegisterInheritedProperties(parentProvider);
					}
					finally
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
			try
			{
				_unregisteringInheritedProperties = true;

				_inheritedForwardedProperties.Clear();

				if (ActualInstance != null)
				{
					if (_updatedProperties is not null)
					{
						// This block is a manual enumeration to avoid the foreach pattern
						// See https://github.com/dotnet/runtime/issues/56309 for details
						var propertiesEnumerator = _updatedProperties.GetEnumerator();
						while (propertiesEnumerator.MoveNext())
						{
							var dp = propertiesEnumerator.Current;
							SetValue(dp, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Inheritance);
						}
					}


					SetValue(_dataContextProperty!, DependencyProperty.UnsetValue, DependencyPropertyValuePrecedences.Inheritance);
				}
			}
			finally
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
			else
			{
				// Look for a property with the same name, even if it is not of the same type
				var localProperty = DependencyProperty.GetProperty(_originalObjectType, property.Name);

				if (localProperty != null)
				{
					var propertyDetails = _properties.GetPropertyDetails(localProperty);

					if (localProperty.IsInherited)
					{
						return (localProperty, propertyDetails);
					}
				}
				else if (property.IsAttached
					&& property.IsInherited

#if __ANDROID__
					// This is a workaround related to property inheritance and
					// https://github.com/unoplatform/uno/pull/18261.
					// Removing this line can randomly produce elements not rendering 
					// properly, such as TextBlock not measure/arrange properly 
					// even when invalidated.
					&& _properties.FindPropertyDetails(property) is { }
#endif
				)
				{
					return (property, _properties.GetPropertyDetails(property));
				}
			}

			return (null, null);
		}

		/// <summary>
		/// Do a tree walk to find the correct values of StaticResource and ThemeResource assignations.
		/// </summary>
		internal void UpdateResourceBindings(ResourceUpdateReason updateReason, FrameworkElement? resourceContextProvider = null, ResourceDictionary? containingDictionary = null)
		{
			if (updateReason == ResourceUpdateReason.None)
			{
				throw new ArgumentException();
			}

			ResourceDictionary[]? dictionariesInScope = null;

			if (updateReason == ResourceUpdateReason.ThemeResource &&
				_properties.HasBindings)
			{
				dictionariesInScope = GetResourceDictionaries(includeAppResources: false, resourceContextProvider, containingDictionary).ToArray();
				for (var i = dictionariesInScope.Length - 1; i >= 0; i--)
				{
					ResourceResolver.PushSourceToScope(dictionariesInScope[i]);
				}

				_properties.UpdateBindingExpressions();

				for (int i = 0; i < dictionariesInScope.Length; i++)
				{
					ResourceResolver.PopSourceFromScope();
				}
			}

			if (_resourceBindings?.HasBindings == true)
			{
				dictionariesInScope ??= GetResourceDictionaries(includeAppResources: false, resourceContextProvider, containingDictionary).ToArray();

				var bindings = _resourceBindings.GetAllBindings();
				foreach (var binding in bindings)
				{
					InnerUpdateResourceBindings(updateReason, dictionariesInScope, binding.Property, binding.Binding);
				}
			}

			UpdateChildResourceBindings(updateReason, resourceContextProvider);
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InnerUpdateResourceBindings(ResourceUpdateReason updateReason, ResourceDictionary[] dictionariesInScope, DependencyProperty property, ResourceBinding binding)
		{
			try
			{
				InnerUpdateResourceBindingsUnsafe(updateReason, dictionariesInScope, property, binding);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
				{
					this.Log().Warn($"Failed to update binding, target may have been disposed", e);
				}
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InnerUpdateResourceBindingsUnsafe(ResourceUpdateReason updateReason, ResourceDictionary[] dictionariesInScope, DependencyProperty property, ResourceBinding binding)
		{
			if ((binding.UpdateReason & updateReason) == ResourceUpdateReason.None)
			{
				// If the reason for the update doesn't match the reason(s) that the binding was created for, don't update it
				return;
			}

			if ((updateReason & ResourceUpdateReason.ResolvedOnLoading) != 0)
			{
				// Add the current dictionaries to the resolver scope,
				// this allows for StaticResource.ResourceKey to resolve properly

				for (var i = dictionariesInScope.Length - 1; i >= 0; i--)
				{
					ResourceResolver.PushSourceToScope(dictionariesInScope[i]);
				}
			}

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

			if (!wasSet)
			{
				if (ResourceResolver.TryTopLevelRetrieval(binding.ResourceKey, binding.ParseContext, out var value))
				{
					SetResourceBindingValue(property, binding, value);
				}
			}

			if ((updateReason & ResourceUpdateReason.ResolvedOnLoading) != 0)
			{
				foreach (var dict in dictionariesInScope)
				{
					ResourceResolver.PopSourceFromScope();
				}
			}
		}

		[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "normal flow of operation")]
		[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "normal flow of operation")]
		private void SetResourceBindingValue(DependencyProperty property, ResourceBinding binding, object? value)
		{
			var convertedValue = BindingPropertyHelper.Convert(property.Type, value);
			if (binding.SetterBindingPath != null)
			{
				try
				{
					_isSettingPersistentResourceBinding = binding.IsPersistent;
					binding.SetterBindingPath.Value = convertedValue;
				}
				finally
				{
					_isSettingPersistentResourceBinding = false;
				}
			}
			else
			{
				SetValue(property, convertedValue, binding.Precedence, isPersistentResourceBinding: binding.IsPersistent);
			}
		}

		private bool _isUpdatingChildResourceBindings;

		private void UpdateChildResourceBindings(ResourceUpdateReason updateReason, FrameworkElement? resourceContextProvider = null)
		{
			if (_isUpdatingChildResourceBindings)
			{
				// Some DPs might be creating reference cycles, so we make sure not to enter an infinite loop.
				return;
			}

			if ((updateReason & ResourceUpdateReason.PropagatesThroughTree) != ResourceUpdateReason.None)
			{
				try
				{
					InnerUpdateChildResourceBindings(updateReason, resourceContextProvider);
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

				_properties.OnThemeChanged();
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InnerUpdateChildResourceBindings(ResourceUpdateReason updateReason, FrameworkElement? resourceContextProvider = null)
		{
			_isUpdatingChildResourceBindings = true;

			foreach (var propertyDetail in _properties.GetAllDetails())
			{
				if (propertyDetail == null
					|| propertyDetail == _properties.DataContextPropertyDetails)
				{
					continue;
				}

				var propertyValue = GetValue(propertyDetail);

				if (propertyValue is IEnumerable<DependencyObject> dependencyObjectCollection &&
					// Try to avoid enumerating collections that shouldn't be enumerated, since we may be encountering user-defined values. This may need to be refined to somehow only consider values coming from the framework itself.
					(propertyValue is ICollection || propertyValue is DependencyObjectCollectionBase)
				)
				{
					foreach (var innerValue in dependencyObjectCollection)
					{
						UpdateResourceBindingsIfNeeded(innerValue, updateReason, resourceContextProvider);
					}
				}

				if (propertyValue is IAdditionalChildrenProvider updateable)
				{
					foreach (var innerValue in updateable.GetAdditionalChildObjects())
					{
						UpdateResourceBindingsIfNeeded(innerValue, updateReason, resourceContextProvider);
					}
				}

				if (propertyValue is DependencyObject dependencyObject)
				{
					UpdateResourceBindingsIfNeeded(dependencyObject, updateReason, resourceContextProvider);
				}
			}
		}

		private void UpdateResourceBindingsIfNeeded(DependencyObject dependencyObject, ResourceUpdateReason updateReason, FrameworkElement? resourceContextProvider = null)
		{
			// propagate to non-FE DO
			if (dependencyObject is not IFrameworkElement && dependencyObject is IDependencyObjectStoreProvider storeProvider)
			{
				storeProvider.Store.UpdateResourceBindings(
					updateReason,
					// when propagating to non-FE, we need to inject a FE as the resource context
					resourceContextProvider: resourceContextProvider ?? ActualInstance as FrameworkElement
				);
			}
		}

		/// <summary>
		/// Returns all ResourceDictionaries in scope using the visual tree, from nearest to furthest.
		/// </summary>
		internal IEnumerable<ResourceDictionary> GetResourceDictionaries(
			bool includeAppResources,
			FrameworkElement? resourceContextProvider = null,
			ResourceDictionary? containingDictionary = null)
		{
			if (containingDictionary is not null)
			{
				yield return containingDictionary;
			}

			// for non-FE, favor context provider over actual-instance
			var candidate = ActualInstance as FrameworkElement ?? resourceContextProvider ?? ActualInstance;
			while (candidate is not null)
			{
				if (candidate is FrameworkElement fe)
				{
					if (fe.TryGetResources() is { IsEmpty: false }) // It's legal (if pointless) on UWP to set Resources to null from user code, so check
					{
						yield return fe.Resources;
					}

					candidate = fe.Parent;
				}
				else
				{
					candidate = VisualTreeHelper.GetParent(candidate);
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
		private void PropagateInheritedProperties(DependencyObjectStore? childStore = null)
		{
			if (childStore is null && _childrenStores.Count == 0)
			{
				// Avoid doing any unnecessary work.
				return;
			}

			// Raise the property change for the current values
			var props = DependencyProperty.GetInheritedPropertiesForType(_originalObjectType);

			// Not using the ActualInstance property here because we need to get a WeakReference instead.
			var instanceRef = _originalObjectRef != null ? _originalObjectRef : ThisWeakReference;

			void Propagate(DependencyObjectStore store)
			{
				for (var propertyIndex = 0; propertyIndex < props.Length; propertyIndex++)
				{
					var prop = props[propertyIndex];

					// The GetValue call here needs to happen regardless of the precedence check.
					// Yes, it may appear like unnecessary work, but it's actually not.
					// The side effect is coming from TryRegisterInheritedProperties call in GetValue.
					var value = GetValue(prop);

					var precedence = GetCurrentHighestValuePrecedence(prop);
					if (precedence is not DependencyPropertyValuePrecedences.DefaultValue)
					{
						store.OnParentPropertyChangedCallback(instanceRef, prop, value);
					}
				}
			}

			if (childStore != null)
			{
				Propagate(childStore);
			}
			else
			{
				var localChildrenStores = _childrenStores;
				for (var childStoreIndex = 0; childStoreIndex < localChildrenStores.Count; childStoreIndex++)
				{
					var child = localChildrenStores[childStoreIndex];
					Propagate(child);
				}
			}

			PropagateInheritedNonLocalProperties(childStore);
		}

		private void PropagateInheritedNonLocalProperties(DependencyObjectStore? childStore)
		{
			if (_inheritedForwardedProperties.Count == 0)
			{
				// Avoid unnecessary AncestorsDictionary allocation and ActualInstance resolution.
				return;
			}

			// Propagate the properties that have been inherited from an other
			// parent, but that are not defined in the current instance.
			// This is used when a child is being added after the parent has already set its inheritable
			// properties.

			// Ancestors is a local cache to avoid walking up the tree multiple times.
			var ancestors = new AncestorsDictionary();

			// This alias is used to avoid the resolution of the underlying WeakReference during the
			// call to IsAncestor.
			var actualInstanceAlias = ActualInstance;

			// This block is a manual enumeration to avoid the foreach pattern
			// See https://github.com/dotnet/runtime/issues/56309 for details
			var forwardedEnumerator = _inheritedForwardedProperties.GetEnumerator();
			while (forwardedEnumerator.MoveNext())
			{
				var sourceInstanceProperties = forwardedEnumerator.Current;

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

							// Get the value from the parent that holds the master value
							(sourceInstanceProperties.Value.Target as DependencyObject)?.GetValue(sourceInstanceProperties.Key)
						);
					}

					if (childStore != null)
					{
						Propagate(childStore);
					}
					else
					{
						var localChildrenStores = _childrenStores;
						for (var i = 0; i < localChildrenStores.Count; i++)
						{
							Propagate(localChildrenStores[i]);
						}
					}
				}
			}

			// Explicit dispose to return HashtableEx's internal array to the pool
			// without having to rely on GC's finalizers.
			ancestors.Dispose();
		}

		private static bool IsAncestor(DependencyObject? instance, AncestorsDictionary map, object ancestor)
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
				// Max iterations for ancestor lookup
				var iterations = 1000;

				while (instance != null && iterations-- > 0)
				{
#if DEBUG
					var prevInstance = instance;
#endif
					instance = DependencyObjectExtensions.GetParent(instance) as DependencyObject;

#if DEBUG
					if (instance != null)
					{
						if (!hashSet.Add(instance))
						{
							throw new Exception($"Cycle detected: [{prevInstance}/{(prevInstance as FrameworkElement)?.Name}] has already added [{instance}/{(instance as FrameworkElement)?.Name}] as parent/");
						}
					}
#endif

					if (instance == ancestor)
					{
						isAncestor = true;
						break;
					}
				}

				if (iterations < 1)
				{
					var level =
#if DEBUG
						// Make layout cycles visible in debug builds, until Children.Add
						// validates for cycles.
						LogLevel.Error;
#else
						// Layout cycles are ignored in this function for release builds.
						LogLevel.Debug;
#endif

					if (ancestor.Log().IsEnabled(level))
					{
						ancestor.Log().Log(level, $"Possible cycle detected, ignoring ancestor [{ancestor}]");
					}

					isAncestor = false;
				}

				map.Set(ancestor, isAncestor);
			}

			return isAncestor;
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
		private static void CreateWeakDelegate(
			PropertyChangedCallback callback,
			out PropertyChangedCallback weakCallback,
			out WeakReferenceReturnDisposable weakRelease)
		{
			var wr = WeakReferencePool.RentWeakReference(null, callback);

			weakCallback =
				(s, e) => (!wr.IsDisposed ? wr.Target as PropertyChangedCallback : null)?.Invoke(s, e);

			weakRelease = new WeakReferenceReturnDisposable(wr);
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
		private static void CreateWeakDelegate(
			ExplicitPropertyChangedCallback callback,
			out ExplicitPropertyChangedCallback weakDelegate,
			out WeakReferenceReturnDisposable weakRelease)
		{
			var wr = WeakReferencePool.RentWeakReference(null, callback);

			weakDelegate =
				(instance, s, e) => (!wr.IsDisposed ? wr.Target as ExplicitPropertyChangedCallback : null)?.Invoke(instance, s, e);

			weakRelease = new WeakReferenceReturnDisposable(wr);
		}

		private struct WeakReferenceReturnDisposable
		{
			private readonly ManagedWeakReference _wr;

			public WeakReferenceReturnDisposable(ManagedWeakReference wr)
				=> _wr = wr;

			public void Dispose()
				=> WeakReferencePool.ReturnWeakReference(null, _wr);
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
			else if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug(
					$"Skipped raising PropertyChangedCallbacks because value for property {propertyDetails.Property.OwnerType}.{propertyDetails.Property.Name} remained identical."
				);
			}
		}

		private static DependencyPropertyChangedEventArgsPool _dpChangedEventArgsPool =
			new(Uno.UI.FeatureConfiguration.DependencyProperty.DependencyPropertyChangedEventArgsPoolSize);

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
			//var propertyChangedParams = new PropertyChangedParams(property, previousValue, newValue);
			var propertyMetadata = property.Metadata;

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
					// Uno TODO: What should we do here for non-UIElements (if anything is needed)?
					if (actualInstanceAlias is UIElement elt)
					{
						var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(elt);
						visual.Compositor.InvalidateRender(visual);
					}
				}

				// Raise the property change for generic handlers for inheritance
				if (frameworkPropertyMetadata.Options.HasInherits())
				{
					var localChildrenStores = _childrenStores;
					for (var storeIndex = 0; storeIndex < localChildrenStores.Count; storeIndex++)
					{
						CallChildCallback(localChildrenStores[storeIndex], instanceRef, property, newValue);
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

			var eventArgs = _dpChangedEventArgsPool.Rent();
			eventArgs.PropertyInternal = property;
			eventArgs.OldValueInternal = previousValue;
			eventArgs.NewValueInternal = newValue;
#if __APPLE_UIKIT__ || IS_UNIT_TESTS
			eventArgs.OldPrecedenceInternal = previousPrecedence;
			eventArgs.NewPrecedenceInternal = newPrecedence;
			eventArgs.BypassesPropagationInternal = bypassesPropagation;
#endif

			// Raise the changes for the callback register to the property itself
			if (propertyMetadata.HasPropertyChanged)
			{
				propertyMetadata.RaisePropertyChangedNoNullCheck(actualInstanceAlias, eventArgs);
			}

			// Ensure binding is propagated
			OnDependencyPropertyChanged(propertyDetails, newValue);

			// Raise the common property change callback of WinUI
			// This is raised *after* the data bound properties are updated
			// but before the registered property callbacks
			if (actualInstanceAlias is IDependencyObjectInternal doInternal)
			{
				doInternal.OnPropertyChanged2(eventArgs);
			}

			// Raise the changes for the callbacks register through RegisterPropertyChangedCallback.
			if (propertyDetails.CanRaisePropertyChanged)
			{
				propertyDetails.RaisePropertyChangedNoNullCheck(actualInstanceAlias, eventArgs);
			}

			// Raise the property change for generic handlers
			var currentCallbacks = _genericCallbacks.Data;
			for (var callbackIndex = 0; callbackIndex < currentCallbacks.Length; callbackIndex++)
			{
				var callback = currentCallbacks[callbackIndex];
				callback.Invoke(instanceRef, property, eventArgs);
			}

			// Cleanup to avoid leaks
			eventArgs.OldValueInternal = null;
			eventArgs.NewValueInternal = null;

			_dpChangedEventArgsPool.Return(eventArgs);
		}

		private void CallChildCallback(DependencyObjectStore childStore, ManagedWeakReference instanceRef, DependencyProperty property, object? newValue)
		{
			var propagateUnregistering = (_unregisteringInheritedProperties || _parentUnregisteringInheritedProperties) && property == _dataContextProperty;
			try
			{
				if (propagateUnregistering)
				{
					childStore._parentUnregisteringInheritedProperties = true;
				}

				childStore.OnParentPropertyChangedCallback(instanceRef, property, newValue);
			}
			finally
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
				&& ((propertyDetails.Property.Metadata as FrameworkPropertyMetadata)?.Options.HasAutoConvert() ?? false))
			{
				if (value?.GetType() != propertyDetails.Property.Type)
				{
					value = Convert.ChangeType(value, propertyDetails.Property.Type, CultureInfo.CurrentCulture);
				}
			}

			if (value == null && !propertyDetails.Property.IsTypeNullable)
			{
				// This probably shouldn't exist. We should fix cases that are broken.
				// Most (if not all) broken cases appear to be related to TemplatedParent being null incorrectly when applying styles.
				// This should be re-validated after https://github.com/unoplatform/uno/issues/1621 is fixed.
				value = GetDefaultValue(propertyDetails.Property);
			}

			propertyDetails.SetValue(value, precedence);

			if (value == DependencyProperty.UnsetValue && precedence >= DependencyPropertyValuePrecedences.Local)
			{
				ReevaluateBaseValue(propertyDetails);
			}
		}

		private void ReevaluateBaseValue(DependencyPropertyDetails propertyDetails)
		{
			// When local value or style value are cleared, we want to re-evaluate base value and set the value with the right precedence.
			// The new base value will either be Style (explicit or implicit) or DefaultStyle (aka built-in style)
			var actualInstance = ActualInstance;
			if (actualInstance is FrameworkElement fe && fe.TryGetValueFromStyle(propertyDetails.Property, out var valueFromStyle))
			{
				// NOTE: ExplicitStyle here actually means ExplicitOrImplicitStyle. This will be fixed with https://github.com/unoplatform/uno/pull/15684/
				propertyDetails.SetValue(valueFromStyle, DependencyPropertyValuePrecedences.ExplicitStyle);
			}
			else if (actualInstance is Control control && control.TryGetValueFromBuiltInStyle(propertyDetails.Property, out var valueFromBuiltInStyle))
			{
				// NOTE: ImplicitStyle here actually means DefaultStyle. This will be fixed with https://github.com/unoplatform/uno/pull/15684/
				propertyDetails.SetValue(valueFromBuiltInStyle, DependencyPropertyValuePrecedences.ImplicitStyle);
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

		internal bool HasLocalOrModifierValue(DependencyProperty dp)
		{
			var precedence = GetCurrentHighestValuePrecedence(dp);

			// TODO Uno Specific: The check is a bit different in WinUI, but should have the same effect.
			bool hasLocalValue = !(
				precedence == DependencyPropertyValuePrecedences.DefaultValue ||
				precedence == DependencyPropertyValuePrecedences.DefaultStyle ||
				precedence == DependencyPropertyValuePrecedences.ExplicitStyle ||
				precedence == DependencyPropertyValuePrecedences.ImplicitStyle);

			return hasLocalValue;
		}

		private void OnParentChanged(object? previousParent, object? value)
		{
			if (_parentChangedCallbacks.Data.Length != 0)
			{
				var actualInstanceAlias = ActualInstance;

				if (actualInstanceAlias != null)
				{
					var args = new DependencyObjectParentChangedEventArgs(previousParent, value);

					var currentCallbacks = _parentChangedCallbacks.Data;
					for (var parentCallbackIndex = 0; parentCallbackIndex < currentCallbacks.Length; parentCallbackIndex++)
					{
						var handler = currentCallbacks[parentCallbackIndex];
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
						Application.PropagateResourcesChanged(frameworkElement, ResourceUpdateReason.ThemeResource);
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

			public override bool Equals(object? obj)
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

			public bool Equals(WeakReference? x, WeakReference? y)
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
