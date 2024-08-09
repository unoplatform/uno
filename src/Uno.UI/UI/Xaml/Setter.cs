#nullable enable

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml
{
	public delegate object SetterValueProviderHandler();
	public delegate object SetterValueProviderHandlerWithOwner(object? owner);

	[DebuggerDisplay("{DebuggerDisplay}")]
	public sealed partial class Setter : SetterBase
	{
		public Setter()
		{

		}

		private BindingPath? _bindingPath;
		private readonly SetterValueProviderHandler? _valueProvider;
		private object? _value;
		private int _targetNameResolutionFailureCount;
		private DependencyProperty? _property;
		private TargetPropertyPath? _target;

		// This property is not part of the WinUI API, but is 
		// required to determine if the value has a binding set
		private static DependencyProperty InternalValueProperty { get; }
			= DependencyProperty.Register(
				nameof(Value),
				typeof(object),
				typeof(Setter),
				new FrameworkPropertyMetadata(default(object)));

		public object? Value
		{
			get
			{
				if (_valueProvider != null)
				{
					return _valueProvider();
				}

				return _value;
			}
			set
			{
				if (!IsMutable)
				{
					ValidateIsSealed();
				}

				_value = value;
			}
		}

		internal bool IsMutable { get; set; }

		private void ValidateIsSealed()
		{
			if (IsSealed)
			{
				throw new InvalidOperationException($"The setter is sealed and cannot be modified");
			}
		}

		public TargetPropertyPath? Target
		{
			get => _target;
			set
			{
				ValidateIsSealed();
				_target = value;
			}
		}

		/// <summary>
		/// The property being set by this setter
		/// </summary>
		public DependencyProperty? Property
		{
			get => _property;
			set
			{
				ValidateIsSealed();
				_property = value;
			}
		}

		/// <summary>
		/// The name of the ThemeResource applied to the value, if any, as an optimized key.
		/// </summary>
		internal SpecializedResourceDictionary.ResourceKey? ThemeResourceKey { get; set; }

		internal XamlParseContext? ThemeResourceContext { get; set; }

		internal ResourceUpdateReason ResourceBindingUpdateReason { get; set; }

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

		public Setter(DependencyProperty targetProperty, object? owner, SetterValueProviderHandlerWithOwner valueProvider)
		{
			Property = targetProperty;

			var ownerRef = WeakReferencePool.RentWeakReference(this, owner);
			_valueProvider = () => valueProvider(ownerRef?.Target);
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
				if (ThemeResourceKey.HasValue)
				{
					ResourceResolver.ApplyResource(o, Property, ThemeResourceKey.Value, ResourceBindingUpdateReason, context: ThemeResourceContext, precedence: null);
				}
				else
				{
					object? value = _valueProvider != null ? _valueProvider() : _value;
					o.SetValue(Property, BindingPropertyHelper.Convert(Property.Type, value));
				}
			}
			else
			{
				throw new NotSupportedException();
			}
		}

		internal void ApplyValue(IFrameworkElement owner)
		{
			var path = TryGetOrCreateBindingPath(owner);

			if (path != null)
			{
				RefreshBindingPath();

				if (ThemeResourceKey.HasValue && ResourceResolver.ApplyVisualStateSetter(ThemeResourceKey.Value, ThemeResourceContext, path, DependencyPropertyValuePrecedences.Animations, ResourceBindingUpdateReason))
				{
					// Applied as theme binding, no need to do more
					return;
				}
				else
				{
					path.Value = Value;
				}
			}
		}

		private void RefreshBindingPath()
			// force binding value to re-evaluate the source and use converters
			=> GetBindingExpression(InternalValueProperty)?.RefreshTarget();

		internal BindingPath? TryGetOrCreateBindingPath(IFrameworkElement owner)
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

			_bindingPath = new BindingPath(path: Target.Path, fallbackValue: null, precedence: DependencyPropertyValuePrecedences.Animations, allowPrivateMembers: false);

			if (Target.Target is ElementNameSubject subject)
			{
				if (subject.ActualElementInstance is ElementStub stub)
				{
					stub.Materialize();
				}

				subject.ElementInstanceChanged += (s, value) =>
				{
					_bindingPath.DataContext = value;
				};

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

		private string DebuggerDisplay => $"Property={Property?.Name ?? "<null>"},Target={Target?.Target?.ToString() ?? Target?.TargetName ?? "<null>"},Value={Value?.ToString() ?? "<null>"}";
	}
}
