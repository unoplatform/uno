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
using Microsoft.UI.Xaml.Data;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.UI.Xaml
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

		// Deliberately not named ValueProperty: the XAML generator treats a "<Name>Property"
		// member as proof that Name is DP-backed, and would route Setter.Value through
		// SetValue/SetBinding — but the value lives in _value, so it would be dropped.
		// WinUI does back Value with a DP; aligning is tracked by uno-private#2292.
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

		[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Types manipulated here have been marked earlier")]
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

				path.Value = Value;
			}
		}

		[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Types manipulated here have been marked earlier")]
		internal override bool TryGetSetterValue(out object? value)
		{
			if (ThemeResourceKey.HasValue)
			{
				if (ResourceResolver.TryStaticRetrieval(ThemeResourceKey.Value, ThemeResourceContext, out value))
				{
					value = BindingPropertyHelper.Convert(Property!.Type, value);
					return true;
				}

				value = null;
				return false;
			}
			else
			{
				value = _valueProvider != null ? _valueProvider() : _value;
				value = BindingPropertyHelper.Convert(Property!.Type, value);
				return true;
			}
		}

		private void RefreshBindingPath()
			// force binding value to re-evaluate the source and use converters
			=> GetBindingExpressionInternal(InternalValueProperty)?.RefreshTarget();

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

			_bindingPath = new BindingPath(path: Target.Path?.Path ?? "", fallbackValue: null, forAnimations: true, allowPrivateMembers: false);

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

		/// <summary>
		/// Queues the visual state setter operation just performed to run again on the next tick, as WinUI does
		/// (VisualStateSetterHelper::PerformSetterOperation, MSFT:11036284): a storyboard stopped in between, like the
		/// visual transition that preceded the state, must not clear the value of the setter.
		/// </summary>
		internal void EnqueueDeferredSetterOperation(IFrameworkElement owner, Media.Animation.DeferredAnimationOperation.DeferredOperation operation, Action? reapplyThemeResource = null)
		{
			if (_bindingPath is null)
			{
				return;
			}

			IBindingItem? leaf = null;
			foreach (var item in _bindingPath.GetPathItems())
			{
				leaf = item;
			}

			if (leaf?.DataContext is not DependencyObject targetObject
				|| string.IsNullOrEmpty(leaf.PropertyName)
				|| DependencyProperty.GetProperty(targetObject.GetType(), leaf.PropertyName) is not { } targetProperty)
			{
				return;
			}

			var setterValue = DependencyProperty.UnsetValue;
			if (operation == Media.Animation.DeferredAnimationOperation.DeferredOperation.Set)
			{
				setterValue = targetObject.GetAnimatedValue(targetProperty);
				if (setterValue == DependencyProperty.UnsetValue)
				{
					return;
				}
			}

			// TODO Uno: WinUI defers custom properties entirely; Uno performs every setter operation synchronously.
			var core = Uno.UI.Xaml.Core.CoreServices.Instance;
			core.RemoveMatchingDeferredAnimationOperations(targetObject, targetProperty);
			core.EnqueueDeferredAnimationOperation(new((targetObject, targetProperty), setterValue, operation, this, ThemeResourceKey.HasValue ? reapplyThemeResource : null));

			// Schedule a tick to ensure the deferred animation operation queue gets flushed
			Media.Animation.TimeManager.RequestAdditionalFrame(0);
		}

		private string DebuggerDisplay => $"Property={Property?.Name ?? "<null>"},Target={Target?.Target?.ToString() ?? Target?.TargetName ?? "<null>"},Value={Value?.ToString() ?? "<null>"}";
	}
}
