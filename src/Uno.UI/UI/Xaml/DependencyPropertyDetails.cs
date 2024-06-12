#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Uno.Buffers;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml
{
	/// <summary>
	/// Represents a stack of values used by the Dependency Property Value Precedence system
	/// </summary>
	internal class DependencyPropertyDetails : IEnumerable<object?>, IEnumerable
	{
		DependencyPropertyValueStack _stack;

		private readonly DependencyProperty _property;
		private readonly Type _dependencyObjectType;
		private PropertyMetadata? _metadata;

		private BindingExpression? _binding;
		private DependencyPropertyCallbackManager? _callbackManager;

		private DependencyPropertyValuePrecedences _highestPrecedence;
		private bool _defaultValueSet;
		private Flags _flags;
		private bool _stackWasUpgraded;

		private static int _localCanDefeatAnimationSuppressed;

		private const int DefaultValueIndex = (int)DependencyPropertyValuePrecedences.DefaultValue;
		private const int LocalValueIndex = (int)DependencyPropertyValuePrecedences.Local;
		private const int StackSize = DefaultValueIndex + 1;

		internal DependencyPropertyDetails(DependencyProperty property, Type dependencyObjectType, bool isTemplatedParentOrDataContext)
		{
			_property = property;

			_dependencyObjectType = dependencyObjectType;

			_highestPrecedence = DependencyPropertyValuePrecedences.DefaultValue;

			ConfigureFlags(property, isTemplatedParentOrDataContext);
		}

		internal void CloneToForHotReload(DependencyPropertyDetails other)
		{
			// If the old instance has a local value **and** the new instance doesn't, then copy the local value.
			// We shouldn't be copying local value if the new instance already has it set as it's likely to be more correct.
			var oldValue = GetValue(DependencyPropertyValuePrecedences.Local);

			if (oldValue != DependencyProperty.UnsetValue &&
				other.GetValue(DependencyPropertyValuePrecedences.Local) == DependencyProperty.UnsetValue)
			{
				other.SetValue(oldValue, DependencyPropertyValuePrecedences.Local);
			}
		}

		private void ConfigureFlags(
			DependencyProperty property,
			bool isTemplatedParentOrDataContext)
		{
			if (isTemplatedParentOrDataContext)
			{
				// TemplatedParent is a DependencyObject but does not propagate datacontext
				_flags = property.HasWeakStorage ?
					Flags.Inherits | Flags.ValueDoesNotInherit | Flags.WeakStorage :
					Flags.Inherits | Flags.ValueDoesNotInherit;

				return;
			}

			if (Metadata is FrameworkPropertyMetadata propertyMetadata)
			{
				const FrameworkPropertyMetadataOptions options =
					FrameworkPropertyMetadataOptions.Inherits |
					FrameworkPropertyMetadataOptions.ValueInheritsDataContext |
					FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext;

				_flags = property.HasWeakStorage ?
					((byte)(propertyMetadata.Options & options)) + Flags.WeakStorage :
					((Flags)(propertyMetadata.Options & options));

				return;
			}

			_flags = property.HasWeakStorage ? Flags.WeakStorage : Flags.None;
		}

		public void Dispose()
		{
			_callbackManager?.Dispose();

			if (_stackWasUpgraded)
			{
				ref var stack = ref Unsafe.As<DependencyPropertyValueStack, object?>(ref _stack);

				for (var i = 0; i < DefaultValueIndex; i++)
				{
					Unsafe.Add(ref stack, i) = null;
				}
			}
		}

		internal object? GetDefaultValue()
		{
			ref var defaultValue = ref Unsafe.Add(ref Unsafe.As<DependencyPropertyValueStack, object?>(ref _stack), DefaultValueIndex);

			if (_defaultValueSet)
			{
				return defaultValue;
			}
			else
			{
				defaultValue = Metadata.DefaultValue;

				// Ensures that the default value of non-nullable properties is not null
				if (defaultValue == null && !_property.IsTypeNullable)
				{
					defaultValue = _property.GetFallbackDefaultValue();
				}

				_defaultValueSet = true;

				return defaultValue;
			}
		}

		private void InitializeDefaultValue(ref object? defaultValue)
		{
			defaultValue = Metadata.DefaultValue;

			// Ensures that the default value of non-nullable properties is not null
			if (defaultValue == null && !_property.IsTypeNullable)
			{
				defaultValue = _property.GetFallbackDefaultValue();
			}

			_defaultValueSet = true;
		}

		public IEnumerator<object?> GetEnumerator()
		{
			if (!_stackWasUpgraded)
			{
				UpgradeStack();
			}

			foreach (var value in _stack)
			{
				yield return value;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			if (!_stackWasUpgraded)
			{
				UpgradeStack();
			}

			foreach (var value in _stack)
			{
				yield return value;
			}
		}

		/// <summary>
		/// Gets the value at the current highest precedence level
		/// </summary>
		internal object? GetValue()
			// Comment originates from WinUI source code (CModifiedValue::GetEffectiveValue)
			// If a local value has been set after an animated value, the local
			// value has precedence. This is different from WPF and is done because
			// some legacy SL apps depend on this and because SL Animation thinks that
			// it is a better design for an animation in a filling period to be trumped by a
			// local value. In the active period of an animation, the next animated
			// value will take precedence over the old local value.
			=> GetValue(_highestPrecedence == DependencyPropertyValuePrecedences.Animations && (_flags & Flags.LocalValueNewerThanAnimationsValue) != 0
				? DependencyPropertyValuePrecedences.Local
				: _highestPrecedence);

		/// <summary>
		/// Gets the value at a given precedence level
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal object? GetValue(DependencyPropertyValuePrecedences precedence)
		{
			if (!_stackWasUpgraded)
			{
				return precedence switch
				{
					DependencyPropertyValuePrecedences.DefaultValue => GetDefaultValue(),
					DependencyPropertyValuePrecedences.Local when _highestPrecedence == DependencyPropertyValuePrecedences.Local => Unwrap(_stack[LocalValueIndex]),
					_ => DependencyProperty.UnsetValue
				};
			}
			else
			{
				if (precedence == DependencyPropertyValuePrecedences.Animations && (_flags & Flags.LocalValueNewerThanAnimationsValue) != 0)
				{
					// When setting BindingPath.Value, we do the following check:
					// DependencyObjectStore.AreDifferent(value, _value.GetPrecedenceSpecificValue())
					// Now, consider the following case:
					// Animation value set to some value x, then Local value is set to some value y,
					// then BindingPath.Value is trying to set Animation value to x
					// in this case, we want to consider the values as different.
					// So, we need to return the Local value when we are asked for Animation as the Local value is effectively overwriting the animation value.
					precedence = DependencyPropertyValuePrecedences.Local;
				}

				return Unwrap(_stack[(int)precedence]);
			}
		}

		/// <summary>
		/// Gets the value at the highest precedence level under the specified one
		/// E.G. If a property has a value both on the Animation, Local and Default
		/// precedences, and the given precedence is Animation, then the Local value is returned.
		/// </summary>
		internal (object? value, DependencyPropertyValuePrecedences precedence) GetValueUnderPrecedence(DependencyPropertyValuePrecedences precedence)
		{
			if (!_stackWasUpgraded)
			{
				if (_highestPrecedence == DependencyPropertyValuePrecedences.Local)
				{
					return (Unwrap(_stack[LocalValueIndex]), DependencyPropertyValuePrecedences.Local);
				}

				return (GetDefaultValue(), DependencyPropertyValuePrecedences.DefaultValue);
			}
			else
			{
				ref var stack = ref Unsafe.As<DependencyPropertyValueStack, object?>(ref _stack);

				// Start from the current precedence + 1 and find the next highest
				for (var i = (int)precedence + 1; i < (int)DependencyPropertyValuePrecedences.DefaultValue; i++)
				{
					var value = Unwrap(Unsafe.Add(ref stack, i));

					if (value != DependencyProperty.UnsetValue)
					{
						return (value, (DependencyPropertyValuePrecedences)i);
					}
				}

				return (Unsafe.Add(ref stack, DefaultValueIndex), DependencyPropertyValuePrecedences.DefaultValue);
			}
		}

		internal IDisposable RegisterCallback(PropertyChangedCallback callback)
			=> (_callbackManager ??= new DependencyPropertyCallbackManager()).RegisterCallback(callback);

		internal void RaisePropertyChanged(DependencyObject actualInstanceAlias, DependencyPropertyChangedEventArgs eventArgs)
			=> _callbackManager?.RaisePropertyChanged(actualInstanceAlias, eventArgs);

		internal BindingExpression? GetBinding() => _binding;

		internal void SetBinding(BindingExpression bindingExpression) => _binding = bindingExpression;

		internal void ClearBinding()
		{
			_binding?.Dispose();
			_binding = null;
		}

		internal void SetSourceValue(object value) => _binding?.SetSourceValue(value);

		internal void SetDefaultValue(object? defaultValue)
		{
			_stack[DefaultValueIndex] = defaultValue;

			_defaultValueSet = true;
		}

		/// <summary>
		/// Sets the value at the given precedence level in the stack
		/// </summary>
		internal void SetValue(object? value, DependencyPropertyValuePrecedences precedence)
		{
			_property.ValidateValue(value);

			var valueIsUnsetValue = value is UnsetValue;

			if (_localCanDefeatAnimationSuppressed == 0 &&
				precedence == DependencyPropertyValuePrecedences.Local &&
				_highestPrecedence == DependencyPropertyValuePrecedences.Animations &&
				!valueIsUnsetValue)
			{
				_flags |= Flags.LocalValueNewerThanAnimationsValue;
			}
			else
			{
				// This might not make much sense, but this is what we are seeing in WinUI code.
				// See https://github.com/unoplatform/uno/issues/5168#issuecomment-1948115761
				_flags &= ~Flags.LocalValueNewerThanAnimationsValue;
			}

			if (!_stackWasUpgraded && precedence != DependencyPropertyValuePrecedences.Local)
			{
				UpgradeStack();
			}

			ref var stackPrecedence = ref Unsafe.Add(ref Unsafe.As<DependencyPropertyValueStack, object?>(ref _stack), (int)precedence);

			if (HasWeakStorage)
			{
				if (stackPrecedence is ManagedWeakReference mwr)
				{
					WeakReferencePool.ReturnWeakReference(this, mwr);
				}

				stackPrecedence =
					value == null && !Property.IsTypeNullable ?
						GetDefaultValue() :
						value != null && value != DependencyProperty.UnsetValue ?
							WeakReferencePool.RentWeakReference(this, value) :
							value;
			}
			else
			{
				stackPrecedence =
					value == null && !Property.IsTypeNullable ?
						GetDefaultValue() :
						value;
			}

			if (!_stackWasUpgraded && precedence == DependencyPropertyValuePrecedences.Local)
			{
				_highestPrecedence = valueIsUnsetValue ?
					DependencyPropertyValuePrecedences.DefaultValue :
					DependencyPropertyValuePrecedences.Local;

				return;
			}

			// After setting the value, we need to update the current highest precedence if needed
			// If a higher value precedence was set, then this is the new highest
			if (!valueIsUnsetValue && precedence < _highestPrecedence)
			{
				_highestPrecedence = precedence;

				return;
			}

			// If we were unsetting the current highest precedence value, we need to find the next highest
			if (valueIsUnsetValue && precedence == _highestPrecedence)
			{
				ref var stack = ref Unsafe.As<DependencyPropertyValueStack, object?>(ref _stack);

				// Start from the current precedence + 1 and find the next highest
				for (var i = (int)precedence + 1; i < (int)DependencyPropertyValuePrecedences.DefaultValue; i++)
				{
					if (Unsafe.Add(ref stack, i) != DependencyProperty.UnsetValue)
					{
						_highestPrecedence = (DependencyPropertyValuePrecedences)i;

						return;
					}
				}

				_highestPrecedence = DependencyPropertyValuePrecedences.DefaultValue;
			}
		}

		internal static void SuppressLocalCanDefeatAnimations() => _localCanDefeatAnimationSuppressed++;

		internal static void ContinueLocalCanDefeatAnimations() => _localCanDefeatAnimationSuppressed--;

		public override string ToString()
		{
			return $"DependencyPropertyDetails({Property.Name})";
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private object? Unwrap(object? value)
			=> HasWeakStorage && value is ManagedWeakReference mwr
			? mwr.Target
			: value;

		private void UpgradeStack()
		{
			ref var stackElement0 = ref Unsafe.As<DependencyPropertyValueStack, object?>(ref _stack);

			var unsetValue = DependencyProperty.UnsetValue;

			stackElement0 = unsetValue;
			Unsafe.Add(ref stackElement0, 1) = unsetValue;

			if (_highestPrecedence != DependencyPropertyValuePrecedences.Local)
			{
				Unsafe.Add(ref stackElement0, LocalValueIndex) = unsetValue;
			}

			Unsafe.Add(ref stackElement0, 3) = unsetValue;
			Unsafe.Add(ref stackElement0, 4) = unsetValue;
			Unsafe.Add(ref stackElement0, 5) = unsetValue;
			Unsafe.Add(ref stackElement0, 6) = unsetValue;
			Unsafe.Add(ref stackElement0, 7) = unsetValue;

			if (!_defaultValueSet)
			{
				InitializeDefaultValue(ref Unsafe.Add<object?>(ref stackElement0, DefaultValueIndex));
			}

			_stackWasUpgraded = true;
		}

		internal DependencyPropertyValuePrecedences CurrentHighestValuePrecedence => _highestPrecedence;

		internal bool HasInherits => (_flags & Flags.Inherits) != 0;

		internal bool HasValueInherits => (_flags & Flags.ValueInherits) != 0;

		internal bool HasValueDoesNotInherit => (_flags & Flags.ValueDoesNotInherit) != 0;

		private bool HasWeakStorage => (_flags & Flags.WeakStorage) != 0;

		public PropertyMetadata Metadata => _metadata ??= _property.GetMetadata(_dependencyObjectType);

		public DependencyProperty Property => _property;

		[InlineArray(StackSize)]
		private struct DependencyPropertyValueStack
		{
#pragma warning disable IDE0051 // Remove unused private members
			private object? _element0;
#pragma warning restore IDE0051 // Remove unused private members
		}

		[Flags]
		private enum Flags : byte
		{
			/// <summary>
			/// No flag is being set
			/// </summary>
			None = 0,

			/// <summary>
			/// Determines if the property inherits Value from its parent
			/// </summary>
			Inherits = 1 << 0,

			/// <summary>
			/// Determines if the property inherits DataContext from its parent
			/// </summary>
			ValueInherits = 1 << 1,

			/// <summary>
			/// This dependency property uses weak storage for its values
			/// </summary>
			WeakStorage = 1 << 2,

			/// <summary>
			/// Determines if the property must not inherit DataContext from its parent
			/// </summary>
			ValueDoesNotInherit = 1 << 3,

			/// <summary>
			/// Normally, Animations has higher precedence than Local.
			/// However, we want local to take higher precedence if it's newer.
			/// This flag records this information.
			/// </summary>
			LocalValueNewerThanAnimationsValue = 1 << 4
		}
	}
}
