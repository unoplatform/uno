using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Data;
using Uno.Collections;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// A <see cref="DependencyPropertyDetails"/> collection
	/// </summary>
	partial class DependencyPropertyDetailsCollection
	{
		private ImmutableList<BindingExpression> _bindings = ImmutableList<BindingExpression>.Empty;
		private ImmutableList<BindingExpression> _templateBindings = ImmutableList<BindingExpression>.Empty;

		private bool _bindingsSuspended;

		/// <summary>
		/// Applies the specified templated parent on the current <see cref="TemplateBinding"/> instances
		/// </summary>
		/// <param name="templatedParent"></param>
		internal void ApplyTemplatedParent(FrameworkElement templatedParent)
		{
			var templateBindings = _templateBindings.Data;

			for (int i = 0; i < templateBindings.Length; i++)
			{
				ApplyBinding(templateBindings[i], templatedParent);
			}
		}

		public bool HasBindings =>
			_bindings != ImmutableList<BindingExpression>.Empty || _templateBindings != ImmutableList<BindingExpression>.Empty;

		/// <summary>
		/// Applies the specified datacontext on the current <see cref="Binding"/> instances
		/// </summary>
		public void ApplyDataContext(object dataContext)
		{
			var bindings = _bindings.Data;

			for (int i = 0; i < bindings.Length; i++)
			{
				ApplyBinding(bindings[i], dataContext);
			}
		}

		/// <summary>
		/// Applies the <see cref="Binding"/> instances which have a <see cref="Binding.CompiledSource"/> set.
		/// </summary>
		internal void ApplyCompiledBindings()
		{
			var bindings = _bindings.Data;

			for (int i = 0; i < bindings.Length; i++)
			{
				bindings[i].ApplyCompiledSource();
			}
		}

		/// <summary>
		/// Applies the <see cref="Binding"/> instances which contain an ElementName property
		/// </summary>
		internal void ApplyElementNameBindings()
		{
			var bindings = _bindings.Data;

			for (int i = 0; i < bindings.Length; i++)
			{
				bindings[i].ApplyElementName();
			}
		}

		/// <summary>
		/// Suspends the <see cref="Binding"/> instances from reacting to DataContext changes
		/// </summary>
		internal void SuspendBindings()
		{
			if (!_bindingsSuspended)
			{
				_bindingsSuspended = true;

				var bindings = _bindings.Data;

				for (int i = 0; i < bindings.Length; i++)
				{
					bindings[i].SuspendBinding();
				}
			}
		}

		/// <summary>
		/// Resumes the <see cref="Binding"/> instances for reacting to DataContext changes
		/// </summary>
		internal void ResumeBindings()
		{
			if (_bindingsSuspended)
			{
				_bindingsSuspended = false;

				var bindings = _bindings.Data;

				for (int i = 0; i < bindings.Length; i++)
				{
					bindings[i].ResumeBinding();
				}

				ApplyDataContext(DataContextPropertyDetails.GetValue());
			}
		}

		/// <summary>
		/// Gets the DataContext <see cref="Binding"/> instance, if any
		/// </summary>
		/// <returns></returns>
		internal BindingExpression FindDataContextBinding() => DataContextPropertyDetails.GetBinding();

		/// <summary>
		/// Sets the specified <paramref name="binding"/> on the <paramref name="target"/> instance.
		/// </summary>
		internal void SetBinding(DependencyProperty dependencyProperty, Binding binding, ManagedWeakReference target)
		{
			if (GetPropertyDetails(dependencyProperty) is DependencyPropertyDetails details)
			{
				// Clear previous binding, to avoid erroneously pushing two-way value to it
				details.ClearBinding();

				var bindingExpression =
					new BindingExpression(
						viewReference: target,
						targetPropertyDetails: details,
						binding: binding
					);

				details.SetBinding(bindingExpression);

				if (Equals(binding.RelativeSource, RelativeSource.TemplatedParent))
				{
					_templateBindings = _templateBindings.Add(bindingExpression);

					ApplyBinding(bindingExpression, TemplatedParentPropertyDetails.GetValue());
				}
				else
				{
					_bindings = _bindings.Add(bindingExpression);

					if (bindingExpression.TargetPropertyDetails.Property.UniqueId == DataContextPropertyDetails.Property.UniqueId)
					{
						bindingExpression.DataContext = details.GetValue(DependencyPropertyValuePrecedences.Inheritance);
					}
					else
					{
						ApplyBinding(bindingExpression, DataContextPropertyDetails.GetValue());
					}
				}
			}
		}

		private void ApplyBinding(BindingExpression binding, object dataContext)
		{
			if (Equals(binding.ParentBinding.RelativeSource, RelativeSource.TemplatedParent))
			{
				binding.DataContext = dataContext;
			}
			else if (Equals(binding.ParentBinding.RelativeSource, RelativeSource.Self))
			{
				binding.DataContext = Owner;
			}
			else
			{
				var isDataContextBinding = binding.TargetPropertyDetails.Property.UniqueId == DataContextPropertyDetails.Property.UniqueId;

				if (!isDataContextBinding)
				{
					binding.DataContext = dataContext;
				}
			}
		}

		/// <summary>
		/// Sets the source value for the the specified <paramref name="property"/>
		/// </summary>
		internal void SetSourceValue(DependencyProperty property, object value)
		{
			if (GetPropertyDetails(property) is DependencyPropertyDetails details)
			{
				SetSourceValue(details, value);
			}
		}

		/// <summary>
		/// Sets the source value for the the specified <paramref name="details"/> 
		/// </summary>
		internal void SetSourceValue(DependencyPropertyDetails details, object value)
		{
			details.SetSourceValue(value);

			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("Set binding value [{0}] from [{1}].", details.Property.Name, _ownerType);
			}
		}

		/// <summary>
		/// Gets the last known defined <see cref="BindingExpression"/> for the specified <paramref name="dependencyProperty"/>
		/// </summary>
		/// <param name="dependencyProperty"></param>
		/// <returns></returns>
		internal BindingExpression GetBindingExpression(DependencyProperty dependencyProperty)
		{
			if (GetPropertyDetails(dependencyProperty) is DependencyPropertyDetails details)
			{
				return details.GetBinding();
			}

			return null;
		}

		internal bool IsPropertyTemplateBound(DependencyProperty dependencyProperty)
		{
			foreach (var templateBinding in _templateBindings)
			{
				if (templateBinding.TargetPropertyDetails.Property == dependencyProperty)
				{
					return true;
				}
			}

			return false;
		}

		internal void UpdateBindingExpressions()
		{
			foreach (var binding in _bindings)
			{
				UpdateBindingPropertiesFromThemeResources(binding.ParentBinding);
			}

			foreach (var binding in _templateBindings)
			{
				UpdateBindingPropertiesFromThemeResources(binding.ParentBinding);
			}
		}

		private static void UpdateBindingPropertiesFromThemeResources(Binding binding)
		{
			if (binding.TargetNullValueThemeResource is { } targetNullValueThemeResourceKey)
			{
				binding.TargetNullValue = (object)ResourceResolverSingleton.Instance.ResolveResourceStatic(targetNullValueThemeResourceKey, typeof(object), context: binding.ParseContext);
			}

			if (binding.FallbackValueThemeResource is { } fallbackValueThemeResourceKey)
			{
				binding.FallbackValue = (object)ResourceResolverSingleton.Instance.ResolveResourceStatic(fallbackValueThemeResourceKey, typeof(object), context: binding.ParseContext);
			}
		}

		internal void OnThemeChanged()
		{
			foreach (var binding in _bindings)
			{
				RefreshBindingValueIfNecessary(binding);
			}

			foreach (var binding in _templateBindings)
			{
				RefreshBindingValueIfNecessary(binding);
			}
		}

		private void RefreshBindingValueIfNecessary(BindingExpression binding)
		{
			if (binding.ParentBinding.TargetNullValueThemeResource is not null ||
				binding.ParentBinding.FallbackValueThemeResource is not null)
			{
				// Note: This may refresh the binding more than really necessary.
				// For example, if TargetNullValue is set to a theme resource, but the binding is not null
				// In this case, a change to TargetNullValue should probably not refresh the binding.
				// Another case is when the ThemeResource evaluates the same between light/dark themes.
				// For now, it's not necessary.
				binding.RefreshTarget();
			}
		}
	}
}
