using System;
using System.Linq;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using Uno.Logging;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI;
using Windows.UI.Xaml.Data;
using System.Collections.Generic;
using Windows.UI.Xaml;
using System.Globalization;
using System.Threading;
using Uno.Diagnostics.Eventing;
using System.Collections;

#if !NET461
using Uno.UI.Controls;
#endif

#if XAMARIN_IOS
using UIKit;
#endif

namespace Windows.UI.Xaml
{
	public partial class DependencyObjectStore
	{
		private delegate void DataContextProviderAction(IDataContextProvider provider);
		private delegate void ObjectAction(object instance);

		private static readonly object[] EmptyChildren = new object[0];

		// Initialize the field with zero capacity, as it may stay empty more often than it is being used.
		private readonly CompositeDisposable _subscriptions = new CompositeDisposable(0);

		private readonly SerialDisposable _propertyChangedSubscription = new SerialDisposable();

		private readonly Dictionary<DependencyProperty, int> _childrenBindableMap = new Dictionary<DependencyProperty, int>(0, DependencyPropertyComparer.Default);
		private readonly List<object> _childrenBindable = new List<object>();

		private bool _isApplyingTemplateBindings;
		private bool _isApplyingDataContextBindings;
		private bool _bindingsSuspended;
		private DependencyProperty _dataContextProperty;
		private DependencyProperty _templatedParentProperty;

		/// <summary>
		/// Sets the templated parent, with the ability to control the propagation of the templated parent.
		/// </summary>
		/// <param name="templatedParent">The parent to apply.</param>
		/// <param name="applyToChildren">
		/// Applies the templated parent to children if true. False is generally used when a control is template-able
		/// to avoid propagating its own templated parent to its children.
		/// </param>
		public void SetTemplatedParent(FrameworkElement templatedParent)
		{
#if !HAS_EXPENSIVE_TRYFINALLY
			// The try/finally incurs a very large performance hit in mono-wasm, and SetValue is in a very hot execution path.
			// See https://github.com/mono/mono/issues/13653 for more details.
			try
#endif
			{
				if (_isApplyingTemplateBindings || _bindingsSuspended)
				{
					// If we reach this point, this means that a propagation loop has been detected, and
					// we can skip the current binder.
					// This can happen if a DependencyObject-typed DependencyProperty contains a reference
					// to one of its ancestors.
					return;
				}

				_isApplyingTemplateBindings = true;

				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().DebugFormat(
						"{0}.ApplyTemplateBindings({1}/{2}) (h:{3:X8})",
						_originalObjectType.ToString(),
						templatedParent?.GetType().ToString() ?? "[null]",
						templatedParent?.GetHashCode().ToString("X8", CultureInfo.InvariantCulture) ?? "[null]",
						ActualInstance?.GetHashCode()
					);
				}

				_properties.ApplyTemplatedParent(templatedParent);

				ApplyChildrenBindable(templatedParent, isTemplatedParent: true);
			}
#if !HAS_EXPENSIVE_TRYFINALLY
			finally
#endif
			{
				_isApplyingTemplateBindings = false;
			}
		}

		private void ApplyChildrenBindable(object inheritedValue, bool isTemplatedParent)
		{
			void SetInherited(IDependencyObjectStoreProvider provider)
			{
				if (isTemplatedParent)
				{
					provider.Store.SetInheritedTemplatedParent(inheritedValue);
				}
				else
				{
					provider.Store.SetInheritedDataContext(inheritedValue);
				}
			}

			for (int i = 0; i < _childrenBindable.Count; i++)
			{
				var child = _childrenBindable[i];

				if (child is IDependencyObjectStoreProvider provider)
				{
					SetInherited(provider);
				}
				else
				{
					// The property value may be an enumerable of providers
					var isValidEnumerable = !(child is string);

					if (
						isValidEnumerable
						&& child is IEnumerable enumerable
					)
					{
						foreach (var item in enumerable)
						{
							if (item is IDependencyObjectStoreProvider provider2)
							{
								SetInherited(provider2);
							}
						}
					}
				}
			}
		}

		private void SetInheritedTemplatedParent(object templatedParent)
			=> SetValue(_templatedParentProperty, templatedParent, DependencyPropertyValuePrecedences.Inheritance, _templatedParentPropertyDetails);

		private void SetInheritedDataContext(object dataContext)
			=> SetValue(_dataContextProperty, dataContext, DependencyPropertyValuePrecedences.Inheritance, _dataContextPropertyDetails);

		/// <summary>
		/// Apply load-time binding updates. Processes the x:Bind markup for the current FrameworkElement, applies load-time ElementName bindings, and updates ResourceBindings.
		/// </summary>
		public void ApplyCompiledBindings()
		{
			_properties.ApplyCompiledBindings();
			InvokeCompiledBindingsCallbacks();
			UpdateResourceBindings();
		}

		private IDisposable RegisterCompiledBindingsUpdates()
			// Compiled bindings propagation is performed through all non-FrameworkElement providers
			// to avoid executing this code twice because all FrameworkElement instances call 
			// ApplyCompiledBindings when FrameworkElement.Loading is raised.
			=> ActualInstance is IFrameworkElement 
				? null 
				: DependencyObjectExtensions.RegisterCompiledBindingsUpdateCallback(Parent, ApplyCompiledBindings);

		private string GetOwnerDebugString()
			=> ActualInstance?.GetType().ToString() ?? "[collected]";

		private void InitializeBinder(DependencyProperty dataContextProperty, DependencyProperty templatedParentProperty)
		{
			_dataContextProperty = dataContextProperty;
			_templatedParentProperty = templatedParentProperty;
		}

		static void InitializeStaticBinder()
		{
			// Register the ability for the BindingPath to subscribe to dependency property changes.
			BindingPath.RegisterPropertyChangedRegistrationHandler(SubscribeToDependencyPropertyChanged);
		}

		internal DependencyProperty DataContextProperty => _dataContextProperty;
		internal DependencyProperty TemplatedParentProperty => _templatedParentProperty;

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
			if (_isDisposed)
			{
				return;
			}

			_subscriptions.Dispose();
			_propertyChangedSubscription.Dispose();
			_properties.Dispose();
			_isDisposed = true;
		}

		private void OnDataContextChanged(object providedDataContext, object actualDataContext, DependencyPropertyValuePrecedences precedence)
		{
			var dataContextBinding = _properties.FindDataContextBinding();

			if (dataContextBinding != null && precedence == DependencyPropertyValuePrecedences.Inheritance)
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
#if !HAS_EXPENSIVE_TRYFINALLY
				// The try/finally incurs a very large performance hit in mono-wasm, and SetValue is in a very hot execution path.
				// See https://github.com/mono/mono/issues/13653 for more details.
				try
#endif
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

					using (TryWriteDataContextChangedEventActivity())
					{
						_properties.ApplyDataContext(actualDataContext);

						ApplyChildrenBindable(actualDataContext, isTemplatedParent: false);
					}
				}
#if !HAS_EXPENSIVE_TRYFINALLY
				finally
#endif
				{
					_isApplyingDataContextBindings = false;
				}
			}
		}

		private IDisposable TryWriteDataContextChangedEventActivity()
		{
			IDisposable traceActivity = null;

			if (_trace.IsEnabled)
			{
				traceActivity = _trace.WriteEventActivity(TraceProvider.DataContextChangedStart, TraceProvider.DataContextChangedStop, GetTraceProperties());
			}

			return traceActivity;
		}

		private object[] GetTraceProperties()
			=> new object[] { ObjectId, _originalObjectType?.ToString() };


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
				_resourceBindings = _resourceBindings ?? new Dictionary<DependencyProperty, ResourceBinding>();
				_resourceBindings[dependencyProperty] = resourceBinding;
			}
			else
			{
				throw new NotSupportedException("Only Windows.UI.Xaml.Data.Binding is supported for bindings.");
			}
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

			var fullBinding = binding as Windows.UI.Xaml.Data.Binding;

			if (fullBinding != null)
			{
				var boundProperty = DependencyProperty.GetProperty(_originalObjectType, dependencyProperty) 
					?? FindStandardProperty(_originalObjectType, dependencyProperty);

				if (boundProperty != null)
				{
					_properties.SetBinding(boundProperty, fullBinding, _originalObjectRef);
				}
			}
			else
			{
				throw new NotSupportedException("Only Windows.UI.Xaml.Data.Binding is supported for bindings.");
			}
		}

		/// <summary>
		/// Finds a DependencyProperty for the specified C# property
		/// </summary>
		private DependencyProperty FindStandardProperty(Type originalObjectType, string dependencyProperty)
		{
			var propertyType = BindingPropertyHelper.GetPropertyType(originalObjectType, dependencyProperty);

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
						new PropertyMetadata(null)
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

		public void SetBindingValue(object value, [CallerMemberName] string propertyName = null)
		{
			var property = DependencyProperty.GetProperty(_originalObjectType, propertyName);

			if(property != null)
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
		internal void SetBindingValue(DependencyPropertyDetails propertyDetails, object value)
		{
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
		private static IDisposable SubscribeToDependencyPropertyChanged(ManagedWeakReference dataContextReference, string propertyName, Action newValueAction)
		{
			var dependencyObject = dataContextReference.Target as DependencyObject;

			if (dependencyObject != null)
			{
				var dp = Windows.UI.Xaml.DependencyProperty.GetProperty(dependencyObject.GetType(), propertyName);

				if (dp != null)
				{
					Windows.UI.Xaml.PropertyChangedCallback handler = (s, e) => newValueAction();

					return Windows.UI.Xaml.DependencyObjectExtensions
						.RegisterDisposablePropertyChangedCallback(dependencyObject, dp, handler);
				}
				else
				{
					if (typeof(DependencyProperty).Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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

		private void OnDependencyPropertyChanged(DependencyPropertyDetails propertyDetails, DependencyPropertyChangedEventArgs args)
		{
			SetBindingValue(propertyDetails, args.NewValue);

			var (hasValueInherits, hasValueDoesNotInherit) = GetPropertyInheritanceConfiguration(propertyDetails);

			if (!hasValueDoesNotInherit && (hasValueInherits || propertyDetails.Property.HasAutoDataContextInherit))
			{
				if(args.NewValue is IDependencyObjectStoreProvider provider)
				{
					_childrenBindable[GetOrCreateChildBindablePropertyIndex(propertyDetails.Property)] = 
						provider.Store.Parent != ActualInstance ? args.NewValue : null;
				}
				else
				{
					_childrenBindable[GetOrCreateChildBindablePropertyIndex(propertyDetails.Property)] = args.NewValue;
				}
			}
		}

		(bool hasValueInherits, bool hasValueDoesNotInherit) GetPropertyInheritanceConfiguration(DependencyPropertyDetails propertyDetails)
		{
			if(
				propertyDetails.Property == _templatedParentProperty 
				|| propertyDetails.Property == _dataContextProperty
			)
			{
				// DataContext is inherited
				// TemplatedParent is a DependencyObject but does not propagate datacontext
				return (false, true);
			}

			if (propertyDetails.Metadata is FrameworkPropertyMetadata propertyMetadata)
			{
				return (
					propertyMetadata.Options.HasValueInheritsDataContext()
					, propertyMetadata.Options.HasValueDoesNotInheritDataContext()
				);
			}

			return (false, false);
		}

		/// <summary>
		/// Gets or create an index in the <see cref="_childrenBindable"/> list, to avoid enumerating <see cref="_childrenBindableMap"/>.
		/// </summary>
		private int GetOrCreateChildBindablePropertyIndex(DependencyProperty property)
		{
			if (!_childrenBindableMap.TryGetValue(property, out int index))
			{
				_childrenBindableMap[property] = index = _childrenBindableMap.Count;
				_childrenBindable.Add(null);
			}

			return index;
		}


		public BindingExpression GetBindingExpression(DependencyProperty dependencyProperty)
			=> _properties.GetBindingExpression(dependencyProperty);

		public Windows.UI.Xaml.Data.Binding GetBinding(DependencyProperty dependencyProperty) 
			=> GetBindingExpression(dependencyProperty)?.ParentBinding;
	}
}

