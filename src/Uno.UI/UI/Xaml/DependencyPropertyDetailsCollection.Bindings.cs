using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml.Data;
using Uno.Collections;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// A <see cref="DependencyPropertyDetails"/> collection
	/// </summary>
	partial class DependencyPropertyDetailsCollection
	{
		private ImmutableList<BindingExpression> _bindings = ImmutableList<BindingExpression>.Empty;

		private bool _bindingsSuspended;

		public bool HasBindings => _bindings != ImmutableList<BindingExpression>.Empty;

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

		internal void SuspendCompiledBindings()
		{
			// ignoring local _bindingsSuspended flag, since this operation is applied on the BindingExpression level.
			var bindings = _bindings.Data;

			for (int i = 0; i < bindings.Length; i++)
			{
				bindings[i].SuspendCompiledSource();
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

		internal void ApplyTemplateBindings()
		{
			var bindings = _bindings.Data;

			for (int i = 0; i < bindings.Length; i++)
			{
				bindings[i].ApplyTemplateBindingParent();
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

				var value = DataContextPropertyDetails.GetEffectiveValue();
				if (value == DependencyProperty.UnsetValue)
				{
					// If we get UnsetValue, it means this is DefaultValue precedence that's not stored in DependencyPropertyDetails.
					// In this case, we know for sure that DataContext's default value is null.
					value = null;
				}

				ApplyDataContext(value);
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
				_bindings = _bindings.Add(bindingExpression);

				if (!Equals(binding.RelativeSource, RelativeSource.TemplatedParent))
				{
					if (bindingExpression.TargetPropertyDetails.Property.UniqueId == DataContextPropertyDetails.Property.UniqueId)
					{
						bindingExpression.DataContext = details.GetInheritedValue();
					}
					else
					{
						var value = DataContextPropertyDetails.GetEffectiveValue();
						if (value == DependencyProperty.UnsetValue)
						{
							// If we get UnsetValue, it means this is DefaultValue precedence that's not stored in DependencyPropertyDetails.
							// In this case, we know for sure that DataContext's default value is null.
							value = null;
						}

						ApplyBinding(bindingExpression, value);
					}
				}
			}
		}

		private void ApplyBinding(BindingExpression binding, object dataContext)
		{
			if (Equals(binding.ParentBinding.RelativeSource, RelativeSource.Self))
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
				this.Log().DebugFormat("Set binding value [{0}].", details.Property.Name);
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
			foreach (var binding in _bindings)
			{
				if (binding.TargetPropertyDetails.Property == dependencyProperty)
				{
					return binding.ParentBinding.IsTemplateBinding;
				}
			}

			return false;
		}

		/// <summary>
		/// True when at least one binding uses a {ThemeResource} for its TargetNullValue or FallbackValue,
		/// i.e. needs re-resolution on a theme change. Lets the theme walk skip the (allocating) scope setup
		/// for the common case of bindings that carry no theme-resolved value.
		/// </summary>
		internal bool HasThemeResourceBindingExpressions
		{
			get
			{
				var bindings = _bindings.Data;
				for (int i = 0; i < bindings.Length; i++)
				{
					var parent = bindings[i].ParentBinding;
					if (parent.TargetNullValueThemeResource is not null || parent.FallbackValueThemeResource is not null)
					{
						return true;
					}
				}

				return false;
			}
		}

		internal void UpdateBindingExpressions()
		{
			foreach (var binding in _bindings)
			{
				if (UpdateBindingPropertiesFromThemeResources(binding.ParentBinding))
				{
					// The binding's theme-resolved TargetNullValue / FallbackValue changed. Re-apply the
					// binding so the new value takes effect: a failing/null binding (e.g. null DataContext)
					// applies its Fallback/TargetNull at activation, which can occur before the theme-resolved
					// value is available, leaving a stale value on the target. Reapply re-runs the full binding
					// (including the null-DataContext fallback path that RefreshTarget skips).
					binding.Reapply();
				}
			}
		}

		// Resolve a binding's TargetNullValue / FallbackValue {ThemeResource} against the binding-target
		// element's effective theme: the caller (DependencyObjectStore.UpdateResourceBindings) scopes it
		// onto the core requested-theme-for-subtree slot, which the dictionary leaf reads to select the
		// Light/Dark sub-dictionary (EnsureActiveThemeDictionary, Resources.cpp:764-768) — matching
		// WinUI's per-owner {ThemeResource} resolution.
		// Returns true when a resolved value actually changed (so the caller can refresh the target).
		private static bool UpdateBindingPropertiesFromThemeResources(Binding binding)
		{
			var changed = false;

			if (binding.TargetNullValueThemeResource is { } targetNullValueThemeResourceKey)
			{
				ResourceResolver.ResolveResourceStatic(targetNullValueThemeResourceKey, out var value, context: binding.ParseContext);
				if (!ReferenceEquals(binding.TargetNullValue, value))
				{
					binding.TargetNullValue = value;
					changed = true;
				}
			}

			if (binding.FallbackValueThemeResource is { } fallbackValueThemeResourceKey)
			{
				ResourceResolver.ResolveResourceStatic(fallbackValueThemeResourceKey, out var value, context: binding.ParseContext);
				if (!ReferenceEquals(binding.FallbackValue, value))
				{
					binding.FallbackValue = value;
					changed = true;
				}
			}

			return changed;
		}
	}
}
