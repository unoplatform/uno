using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.Foundation;
using Windows.Foundation.Metadata;
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
				o.SetValue(Property, BindingPropertyHelper.Convert(() => Property.Type, value));
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		internal void ApplyValue(DependencyPropertyValuePrecedences precedence, IFrameworkElement owner)
		{
			if (_bindingPath == null)
			{
				if (Target != null)
				{
					if (Target.Target == null)
					{
						if (Target.TargetName != null)
						{
							// We're in a late-binding scenario from the XamlReader context.
							// In this case, we don't know the instance in advance, we need
							// to try to resolve the binding path multiple times.

							Target.Target = owner.FindName(Target.TargetName);

							if (Target.Target != null)
							{
								if (this.Log().IsEnabled(LogLevel.Debug))
								{
									this.Log().Debug($"Using Target [{Target.Target}] for Setter [{Target.TargetName}] from [{owner}]");
								}

								BuildBindingPath(precedence);
							}
							else
							{
								if (_targetNameResolutionFailureCount++ > 2)
								{
									if (this.Log().IsEnabled(LogLevel.Warning))
									{
										this.Log().Warn($"Could not find Target [{Target.TargetName}] for Setter [{Target.Path?.Path}] from [{owner}]. This may indicate an invalid Setter name, and can cause performance issues.");
									}
								}
							}
						}
					}
					else
					{
						BuildBindingPath(precedence);
					}
				}
				else
				{
					throw new InvalidOperationException($"Unable to apply setter value with null {nameof(Target)}");
				}
			}

			if (_bindingPath != null)
			{
				_bindingPath.Value = Value;
			}
		}

		private void BuildBindingPath(DependencyPropertyValuePrecedences precedence)
		{
			_bindingPath = new BindingPath(path: Target.Path, fallbackValue: null, precedence: precedence);
			_bindingPath.DataContext = Target.Target;
		}

		internal void ClearValue()
		{
			_bindingPath?.ClearValue();
		}

		private string DebuggerDisplay => $"Property={Property?.Name ?? "<null>"},Target={Target?.Target?.ToString() ?? Target?.TargetName ?? "<null>"},Value={Value?.ToString() ?? "<null>"}";
	}
}
