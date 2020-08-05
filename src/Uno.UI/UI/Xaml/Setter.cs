using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml
{
	public delegate object SetterValueProviderHandler();

	[DebuggerDisplay("{DebuggerDisplay}")]
	public sealed partial class Setter : SetterBase
	{
		public Setter()
		{

		}

		private BindingPath _bindingPath;
		private readonly SetterValueProviderHandler _valueProvider;
		private object _value;
		private int _targetNameResolutionFailureCount;

		public object Value
		{
			get
			{
				if (_valueProvider != null)
				{
					return _valueProvider();
				}

				return _value;
			}
			set => _value = value;
		}

		public TargetPropertyPath Target
		{
			get;
			set;
		}

		/// <summary>
		/// The property being set by this setter
		/// </summary>
		public DependencyProperty Property { get; set; }

		/// <summary>
		/// The name of the ThemeResource applied to the value, if any.
		/// </summary>
		internal string ThemeResourceName { get; set; }

		internal XamlParseContext ThemeResourceContext { get; set; }

		public Setter(DependencyProperty targetProperty, object value)
		{
			Property = targetProperty;
			_value = value;
		}

		public Setter(DependencyProperty targetProperty, SetterValueProviderHandler valueProvider)
		{
			Property = targetProperty;
			_valueProvider = valueProvider;
		}

		public Setter(TargetPropertyPath targetPath, object value)
		{
			Target = targetPath;
			Value = value;
		}

		internal override void ApplyTo(DependencyObject o)
		{
			if (Property != null)
			{
				object value = _valueProvider != null ? _valueProvider() : _value;
				if (!ThemeResourceName.IsNullOrEmpty())
				{
					ResourceResolver.ApplyResource(o, Property, ThemeResourceName, isThemeResourceExtension: true, context: ThemeResourceContext);
				}
				else
				{
					o.SetValue(Property, BindingPropertyHelper.Convert(() => Property.Type, value));
				}
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		internal void ApplyValue(DependencyPropertyValuePrecedences precedence, IFrameworkElement owner)
		{
			var path = TryGetOrCreateBindingPath(precedence, owner);

			if (path != null)
			{
				if (!ThemeResourceName.IsNullOrEmpty())
				{
					// TODO: We should be trying to resolve the leaf DP pointed to by path, and set a theme binding on it
					var value = ResourceResolver.ResolveResourceStatic<object>(ThemeResourceName, context: ThemeResourceContext);
					path.Value = value ?? Value;
				}
				else
				{
					path.Value = Value; 
				}
			}
		}

		private BindingPath TryGetOrCreateBindingPath(DependencyPropertyValuePrecedences precedence, IFrameworkElement owner)
		{
			if (_bindingPath != null)
			{
				return _bindingPath;
			}

			if (Target == null)
			{
				// Ignore a null setter, act as a no-op.
				return null;
			}

			if (Target.Target == null && Target.TargetName != null)
			{
				// We're in a late-binding scenario from the XamlReader context.
				// In this case, we don't know the instance in advance, we need
				// to try to resolve the binding path multiple times.
				Target.Target = owner.FindName(Target.TargetName);

				if (Target.Target == null)
				{
					if (_targetNameResolutionFailureCount++ > 2 && this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"Could not find Target [{Target.TargetName}] for Setter [{Target.Path?.Path}] from [{owner}]. This may indicate an invalid Setter name, and can cause performance issues.");
					}

					return null;
				}
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Using Target [{Target.Target}] for Setter [{Target.TargetName}] from [{owner}]");
			}

			_bindingPath = new BindingPath(path: Target.Path, fallbackValue: null, precedence: precedence, allowPrivateMembers: false);

			if (Target.Target is ElementNameSubject subject)
			{
				if (subject.ActualElementInstance is ElementStub stub)
				{
					stub.Materialize();
				}

				_bindingPath.DataContext = subject.ElementInstance;
			}
			else
			{
				_bindingPath.DataContext = Target.Target;
			}

			return _bindingPath;
		}

		internal void ClearValue()
		{
			_bindingPath?.ClearValue();
		}

		internal bool HasSameTarget(Setter other, DependencyPropertyValuePrecedences precedence, IFrameworkElement owner)
		{
			var path = TryGetOrCreateBindingPath(precedence, owner);
			var otherPath = other.TryGetOrCreateBindingPath(precedence, owner);

			return path != null
				&& otherPath != null
				&& path.Path == otherPath.Path
				&& !DependencyObjectStore.AreDifferent(path.DataContext, otherPath.DataContext);
		}

		private string DebuggerDisplay => $"Property={Property?.Name ?? "<null>"},Target={Target?.Target?.ToString() ?? Target?.TargetName ?? "<null>"},Value={Value?.ToString() ?? "<null>"}";
	}
}
