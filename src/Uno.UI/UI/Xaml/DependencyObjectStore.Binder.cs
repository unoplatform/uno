#nullable enable

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
// fallback option for legacy DepObj from external library generated before templated-parent rework.
#define ENABLE_LEGACY_DO_TP_SUPPORT
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Uno.Collections;
using Uno.Diagnostics.Eventing;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;

#if !IS_UNIT_TESTS
using Uno.UI.Controls;
#endif

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Microsoft.UI.Xaml
{
	public partial class DependencyObjectStore
	{
		private readonly object _gate = new object();

		private HashtableEx? _childrenBindableMap;
		private List<object?>? _childrenBindable;

		private HashtableEx ChildrenBindableMap => _childrenBindableMap ??= new HashtableEx(DependencyPropertyComparer.Default);
		private List<object?> ChildrenBindable => _childrenBindable ??= new List<object?>();

		private bool _isApplyingDataContextBindings;
		private bool _bindingsSuspended;
		private readonly DependencyProperty _dataContextProperty;

#if ENABLE_LEGACY_DO_TP_SUPPORT
		private ManagedWeakReference? _templatedParentWeakRef;
		internal ManagedWeakReference? GetTemplatedParentWeakRef() => _templatedParentWeakRef;
#endif

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
		public void SetTemplatedParent(FrameworkElement? templatedParent)
		{
			// do nothing, this only exist to keep public api the same.
		}
#endif

#if ENABLE_LEGACY_DO_TP_SUPPORT
		internal DependencyObject? GetTemplatedParent2() => _templatedParentWeakRef?.Target as DependencyObject;
		internal void SetTemplatedParent2(DependencyObject parent) => _templatedParentWeakRef = (parent as IWeakReferenceProvider)?.WeakReference;
#endif

		private bool IsCandidateChild([NotNullWhen(true)] object? child)
		{
			if (FeatureConfiguration.DependencyProperty.LegacyDataContextPropagation)
			{
				if (child is IDependencyObjectStoreProvider) return true;
			}
			else
			{
				if (child is FrameworkElement) return true;
			}

			// The property value may be an enumerable of providers
			return (child is not string && child is IEnumerable);
		}

		private void ApplyChildrenBindable(object? inheritedValue)
		{
			if (_childrenBindable is null)
			{
				return;
			}

			for (int i = 0; i < _childrenBindable.Count; i++)
			{
				var child = _childrenBindable[i];
				if (child is null)
				{
					continue;
				}

				Debug.Assert(IsCandidateChild(child));

				var childAsStoreProvider = child as IDependencyObjectStoreProvider;

				// Get the parent if the child is a provider, otherwise an
				// "attached store" may be created for no good reason.
				var parent = childAsStoreProvider?.GetParent();

				//Do not propagate value if you are not this child's parent
				//Covers case where a child may hold a binding to a view higher up the tree
				//Example: Button A contains a Flyout with Button B inside of it
				//	Button B has a binding to the Flyout itself
				//	We should not propagate Button B's DataContext to the Flyout
				//	since its real parent is actually Button A
				if (parent != null && parent != ActualInstance)
				{
					continue;
				}

				static void InnerApply(object? instance, object? value)
				{
					if (!FeatureConfiguration.DependencyProperty.LegacyDataContextPropagation)
					{
						instance = instance as FrameworkElement;
					}

					if (instance is IDependencyObjectStoreProvider provider)
					{
						provider.Store.SetInheritedDataContext(value);
					}
				}

				if (childAsStoreProvider != null)
				{
					InnerApply(childAsStoreProvider, inheritedValue);
				}
				else if (child is IList list)
				{
					// Special case for IList where the child may not be enumerable
					for (int childIndex = 0; childIndex < list.Count; childIndex++)
					{
						InnerApply(list[childIndex], inheritedValue);
					}
				}
				else if (child is IEnumerable enumerable)
				{
					foreach (var item in enumerable)
					{
						InnerApply(item, inheritedValue);
					}
				}
				else
				{
					throw new Exception("This cannot be reached. IsCandidateChild would have returned false if none of the conditions were true.");
				}
			}
		}

		private void SetInheritedDataContext(object? dataContext)
			=> SetValue(_dataContextProperty!, dataContext, DependencyPropertyValuePrecedences.Inheritance, _properties.DataContextPropertyDetails);

		/// <summary>
		/// Apply load-time binding updates. Processes the x:Bind markup for the current FrameworkElement, applies load-time ElementName bindings, and updates ResourceBindings.
		/// </summary>
		public void ApplyCompiledBindings()
			=> _properties.ApplyCompiledBindings();

		public void SuspendCompiledBindings()
			// ignoring local _bindingsSuspended flag, since this operation is applied on the BindingExpression level.
			=> _properties.SuspendCompiledBindings();

		/// <summary>
		/// Apply load-time binding updates. Processes the x:Bind markup for the current FrameworkElement, applies load-time ElementName bindings, and updates ResourceBindings.
		/// </summary>
		internal void ApplyElementNameBindings()
			=> _properties.ApplyElementNameBindings();

		internal void ApplyTemplateBindings()
			// Update the template-bindings when the templated-parent is late injected.
			// Such as the case with: ElementStub materialization.
			=> _properties.ApplyTemplateBindings();

		static void InitializeStaticBinder()
		{
			// Register the ability for the BindingPath to subscribe to dependency property changes.
			BindingPath.RegisterPropertyChangedRegistrationHandler(new BindingPathPropertyChangedRegistrationHandler());
		}

		internal DependencyProperty DataContextProperty => _dataContextProperty!;

		/// <summary>
		/// Restores the bindings that may have been cleared by <see cref="ClearBindings()"/>.
		/// </summary>
		/// <remarks>
		/// Calling this method will specifically restore <see cref="UI.Xaml.Data.Binding.ElementName"/>
		/// and <see cref="UI.Xaml.Data.Binding.Source"/> bindings, which are not restored as part of the
		/// normal <see cref="DataContext"/> change flow.
		/// </remarks>
		public void RestoreBindings()
		{

		}

		/// <summary>
		/// Clears the bindings for the current binder.
		/// </summary>
		/// <remarks>
		/// This method is used as an out-of-band replacement for setting the DataContext to null, which
		/// in the case of two-way bindings, would send the fallback value if it has been set.
		/// This method may also clear <see cref="UI.Xaml.Data.Binding.ElementName"/>
		/// and <see cref="UI.Xaml.Data.Binding.Source"/> bindings, which need to be restored
		/// using the <see cref="RestoreBindings()"/> method.
		/// </remarks>
		public void ClearBindings()
		{

		}

		/// <summary>
		/// Suspends the processing the <see cref="DataContext"/> until <see cref="ResumeBindings"/> is called.
		/// </summary>
		public void SuspendBindings()
		{
			_bindingsSuspended = true;
			_properties.SuspendBindings();
		}

		/// <summary>
		/// Restores the processing the <see cref="DataContext"/> after <see cref="SuspendBindings"/> was called.
		/// </summary>
		public void ResumeBindings()
		{
			_bindingsSuspended = false;
			_properties.ResumeBindings();
		}

		private void BinderDispose()
		{
			lock (_gate)
			{
				// Guard the dispose as it may be invoked from both a finalizer and the dispatcher.
				if (_isDisposed)
				{
					return;
				}

				_isDisposed = true;
			}

			_properties.Dispose();
			_childrenBindableMap?.Dispose();
		}

		private void OnDataContextChanged(object? providedDataContext, object? actualDataContext, DependencyPropertyValuePrecedences precedence)
		{
			if (precedence == DependencyPropertyValuePrecedences.Inheritance && _properties.FindDataContextBinding() is { } dataContextBinding)
			{
				// Set the DataContext for the bindings using the current DataContext, except for the
				// binding to the DataContext itself, which must use the inherited DataContext.
				//
				// This is to avoid recursion when the datacontext.
				//
				// The inherited DataContext may also be null in the case of non-inherited data binding
				// when using the custom target parameter in SetBinding.
				if (dataContextBinding.ParentBinding.RelativeSource == null)
				{
					dataContextBinding.DataContext = providedDataContext;
				}
			}
			else
			{
				try
				{
					if (_isApplyingDataContextBindings || _bindingsSuspended)
					{
						// If we reach this point, this means that a propagation loop has been detected, and
						// we can skip the current binder.
						// This can happen if a DependencyObject-typed DependencyProperty contains a reference
						// to one of its ancestors.
						return;
					}

					_isApplyingDataContextBindings = true;

					if (TryWriteDataContextChangedEventActivity() is { } trace)
					{
						using (trace)
						{
							ApplyDataContext(actualDataContext);
						}
					}
					else
					{
						ApplyDataContext(actualDataContext);
					}
				}
				finally
				{
					_isApplyingDataContextBindings = false;
				}
			}

		}

		private void ApplyDataContext(object? actualDataContext)
		{
			_properties.ApplyDataContext(actualDataContext);
			ApplyChildrenBindable(actualDataContext);
		}

		private IDisposable? TryWriteDataContextChangedEventActivity()
		{
			IDisposable? traceActivity = null;

			if (_trace.IsEnabled)
			{
				traceActivity = _trace.WriteEventActivity(TraceProvider.DataContextChangedStart, TraceProvider.DataContextChangedStop, GetTraceProperties());
			}

			return traceActivity;
		}

		private object?[] GetTraceProperties()
			=> new object?[] { GetHashCode(), _originalObjectType?.ToString() };


		public void SetBinding(object target, string dependencyProperty, BindingBase binding)
		{
			TryRegisterInheritedProperties(force: true);

			if (target is IDependencyObjectStoreProvider provider)
			{
				provider.Store.SetBinding(dependencyProperty, binding);
			}
			else
			{
				throw new NotSupportedException($"Target {target?.GetType()} must be a DependencyObject");
			}
		}

		/// <summary>
		/// Set a binding using a regular or attached DependencyProperty
		/// </summary>
		/// <param name="dependencyProperty">The dependency property to bind</param>
		/// <param name="binding">The binding expression</param>
		public void SetBinding(DependencyProperty dependencyProperty, BindingBase binding)
		{
			TryRegisterInheritedProperties(force: true);

			if (dependencyProperty == null)
			{
				throw new ArgumentNullException(nameof(dependencyProperty));
			}

			if (binding == null)
			{
				throw new ArgumentNullException(nameof(binding));
			}

			if (binding is Binding fullBinding)
			{
				_properties.SetBinding(dependencyProperty, fullBinding, _originalObjectRef);
			}
			else if (binding is ResourceBinding resourceBinding)
			{
				_resourceBindings = _resourceBindings ?? new ResourceBindingCollection();
				_resourceBindings.Add(dependencyProperty, resourceBinding);
			}
			else
			{
				throw new NotSupportedException("Only Microsoft.UI.Xaml.Data.Binding is supported for bindings.");
			}
		}

		internal void SetResourceBinding(DependencyProperty dependencyProperty, SpecializedResourceDictionary.ResourceKey resourceKey, ResourceUpdateReason updateReason, object context, DependencyPropertyValuePrecedences? precedence, BindingPath? setterBindingPath)
		{
			if (precedence == null && _overriddenPrecedences?.Count > 0)
			{
				precedence = _overriddenPrecedences.Peek();
			}

			var binding = new ResourceBinding(resourceKey, updateReason, context, precedence ?? DependencyPropertyValuePrecedences.Local, setterBindingPath);
			SetBinding(dependencyProperty, binding);
		}

		public void SetBinding(string dependencyProperty, BindingBase binding)
		{
			TryRegisterInheritedProperties(force: true);

			if (dependencyProperty == null)
			{
				throw new ArgumentNullException(nameof(dependencyProperty));
			}

			if (binding == null)
			{
				throw new ArgumentNullException(nameof(binding));
			}

			var fullBinding = binding as Microsoft.UI.Xaml.Data.Binding;

			if (fullBinding != null)
			{
				var boundProperty = DependencyProperty.GetProperty(_originalObjectType, dependencyProperty)
					?? FindStandardProperty(_originalObjectType, dependencyProperty, fullBinding.IsXBind);

				if (boundProperty != null)
				{
					_properties.SetBinding(boundProperty, fullBinding, _originalObjectRef);
				}
			}
			else
			{
				throw new NotSupportedException("Only Microsoft.UI.Xaml.Data.Binding is supported for bindings.");
			}
		}

		internal void SetTemplateBinding(DependencyProperty targetProperty, DependencyProperty sourceProperty)
		{
			SetBinding(
				targetProperty,
				new Binding
				{
					Path = sourceProperty.Name,
					RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
				}
			);
		}

		/// <summary>
		/// Finds a DependencyProperty for the specified C# property
		/// </summary>
		[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Types manipulated here have been marked earlier")]
		[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Types manipulated here have been marked earlier")]
		private DependencyProperty? FindStandardProperty(Type originalObjectType, string dependencyProperty, bool allowPrivateMembers)
		{
			var propertyType = BindingPropertyHelper.GetPropertyType(originalObjectType, dependencyProperty, allowPrivateMembers);

			if (propertyType != null)
			{
				// This line populates the cache for the getter in the BindingPropertyHelper, making the binder
				// pick it up later on.
				var setter = BindingPropertyHelper.GetValueSetter(originalObjectType, dependencyProperty, true);

				var property = DependencyProperty.GetProperty(originalObjectType, dependencyProperty);

				if (property == null)
				{
					// Create a stub property so the BindingPropertyHelper is able to pick up
					// the plain C# properties.
					property = DependencyProperty.Register(
						dependencyProperty,
						propertyType,
						originalObjectType,
						new FrameworkPropertyMetadata(null)
					);
				}

				return property;
			}
			else
			{
				this.Log().Error($"The property {dependencyProperty} does not exist on {originalObjectType}");
				return null;
			}
		}

		public void SetBindingValue(object value, [CallerMemberName] string? propertyName = null)
		{
			var property = DependencyProperty.GetProperty(_originalObjectType, propertyName);

			if (property == null && propertyName != null)
			{
				property = FindStandardProperty(_originalObjectType, propertyName, false);
			}

			if (property != null)
			{
				SetBindingValue(value, property);
			}
			else
			{
				throw new InvalidOperationException($"Binding to a non-DependencyProperty is not supported ({_originalObjectType}.{propertyName})");
			}
		}

		/// <summary>
		/// Sets the specified source <paramref name="value"/> on <paramref name="property"/>
		/// </summary>
		public void SetBindingValue(object value, DependencyProperty property)
		{
			_properties.SetSourceValue(property, value);
		}

		/// <summary>
		/// Sets the specified source <paramref name="value"/> on <paramref name="property"/>
		/// </summary>
		internal void SetBindingValue(DependencyPropertyDetails propertyDetails, object? value)
		{
			var unregisteringInheritedProperties = _unregisteringInheritedProperties || _parentUnregisteringInheritedProperties;
			if (unregisteringInheritedProperties)
			{
				// This guards against the scenario where inherited DataContext is removed when the view is removed from the visual tree,
				// in which case 2-way bindings should not be updated.
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat("SetSourceValue() not called because inherited property is being unset.");
				}
				return;
			}
			_properties.SetSourceValue(propertyDetails, value);
		}

		/// <summary>
		/// Subscribes to a dependency property changed handler
		/// </summary>
		/// <param name="dataContext">The DataContext that contains propertyName</param>
		/// <param name="propertyName">The property to observe</param>
		/// <param name="newValueAction">The action to execute when a new value is raised</param>
		/// <param name="disposeAction">The action to execute when the listener wants to dispose the subscription</param>
		/// <returns></returns>
		private static IDisposable? SubscribeToDependencyPropertyChanged(ManagedWeakReference dataContextReference, string propertyName, BindingPath.IPropertyChangedValueHandler newValueAction)
		{
			var dependencyObject = dataContextReference.Target as DependencyObject;

			if (dependencyObject != null)
			{
				var dp = Microsoft.UI.Xaml.DependencyProperty.GetProperty(dependencyObject.GetType(), propertyName);

				if (dp != null)
				{
					return Microsoft.UI.Xaml.DependencyObjectExtensions
						.RegisterDisposablePropertyChangedCallback(dependencyObject, dp, newValueAction.NewValue);
				}
				else
				{
					if (typeof(DependencyProperty).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
					{
						typeof(DependencyProperty).Log().DebugFormat(
							"Unable to find the dependency property [{0}] on type [{1}]"
							, propertyName
							, dependencyObject.GetType()
						);
					}
				}
			}

			return null;
		}

		private void OnDependencyPropertyChanged(DependencyPropertyDetails propertyDetails, object? newValue)
		{
			SetBindingValue(propertyDetails, newValue);

			if (!propertyDetails.HasValueDoesNotInherit)
			{
				var newValueAsProvider = newValue as IDependencyObjectStoreProvider;

				if (propertyDetails.HasValueInherits)
				{
					if (newValueAsProvider is not null)
					{
						SetChildrenBindableValue(
							propertyDetails,

							// Ensure DataContext propagation loops cannot happen
							ReferenceEquals(newValueAsProvider.Store.Parent, ActualInstance) ? null : newValue);
					}
					else
					{
						SetChildrenBindableValue(propertyDetails, newValue);
					}
				}
				else
				{
					if (newValueAsProvider is not null
						&& newValueAsProvider is not UIElement)
					{
						SetChildrenBindableValue(propertyDetails, newValueAsProvider);
					}
				}
			}
		}

		private void SetChildrenBindableValue(DependencyPropertyDetails propertyDetails, object? value)
		{
			if (IsCandidateChild(value))
			{
				ChildrenBindable[GetOrCreateChildBindablePropertyIndex(propertyDetails.Property)] = value;
			}
			else if (TryGetChildBindablePropertyIndex(propertyDetails.Property, out var index))
			{
				ChildrenBindable[index] = null;
			}
		}

		/// <summary>
		/// Gets or create an index in the <see cref="_childrenBindable"/> list, to avoid enumerating <see cref="_childrenBindableMap"/>.
		/// </summary>
		private int GetOrCreateChildBindablePropertyIndex(DependencyProperty property)
		{
			int index;

			if (!ChildrenBindableMap.TryGetValue(property, out var indexRaw))
			{
				ChildrenBindableMap[property] = index = ChildrenBindableMap.Count;
				ChildrenBindable.Add(null); // The caller will replace null with a non-null value.
			}
			else
			{
				index = (int)indexRaw!;
			}

			return index;
		}

		private bool TryGetChildBindablePropertyIndex(DependencyProperty property, out int index)
		{
			if (_childrenBindableMap?.TryGetValue(property, out var indexRaw) == true)
			{
				index = (int)indexRaw!;
				return true;
			}


			index = -1;
			return false;
		}


		public BindingExpression GetBindingExpression(DependencyProperty dependencyProperty)
			=> _properties.GetBindingExpression(dependencyProperty);

		public Microsoft.UI.Xaml.Data.Binding? GetBinding(DependencyProperty dependencyProperty)
			=> GetBindingExpression(dependencyProperty)?.ParentBinding;

		internal bool IsPropertyTemplateBound(DependencyProperty dependencyProperty)
			=> _properties.IsPropertyTemplateBound(dependencyProperty);

		/// <summary>
		/// BindingPath Registration handler for DependencyProperty instances
		/// </summary>
		private class BindingPathPropertyChangedRegistrationHandler : BindingPath.IPropertyChangedRegistrationHandler
		{
			public IDisposable? Register(ManagedWeakReference dataContext, string propertyName, BindingPath.IPropertyChangedValueHandler onNewValue)
				=> SubscribeToDependencyPropertyChanged(dataContext, propertyName, onNewValue);
		}
	}
}

