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
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Represents the stack of values used by the Dependency Property Value Precedence system
	/// </summary>
	internal class DependencyPropertyDetails : IEnumerable<object?>, IEnumerable, IDisposable
	{
		private DependencyPropertyValuePrecedences _highestPrecedence = DependencyPropertyValuePrecedences.DefaultValue;
		private readonly Type _dependencyObjectType;
		private object? _fastLocalValue;
		private BindingExpression? _binding;
		private object?[]? _stack;
		private PropertyMetadata? _metadata;
		private object? _defaultValue;
		private Flags _flags;
		private DependencyPropertyCallbackManager? _callbackManager;

		private const int DefaultValueIndex = (int)DependencyPropertyValuePrecedences.DefaultValue;
		private const int StackSize = DefaultValueIndex + 1;

		private static readonly LinearArrayPool<object?> _pool = LinearArrayPool<object?>.CreateAutomaticallyManaged(StackSize, 1);

		private static readonly object[] _unsetStack;

		private static int _localCanDefeatAnimationSuppressed;

		static DependencyPropertyDetails()
		{
			_unsetStack = new object[StackSize];
			for (var i = 0; i < StackSize; i++)
			{
				_unsetStack[i] = DependencyProperty.UnsetValue;
			}
		}

		internal void CloneToForHotReload(DependencyPropertyDetails other)
		{
			// If the old instance has a local value **and** the new instance doesn't, then copy the local value.
			// We shouldn't be copying local value if the new instance already has it set. The new value in the new instance
			// should not be overwritten as it's more likely to be more correct.
			var oldValue = this.GetValue(DependencyPropertyValuePrecedences.Local);
			if (oldValue != DependencyProperty.UnsetValue &&
				other.GetValue(DependencyPropertyValuePrecedences.Local) == DependencyProperty.UnsetValue)
			{
				other.SetValue(oldValue, DependencyPropertyValuePrecedences.Local);
			}
		}

		public void Dispose()
		{
			_callbackManager?.Dispose();

			if (_stack != null)
			{
				// Note that `clearArray` is required here to avoid pooled arrays to leak
				// instances from set property values.
				_pool.Return(_stack, clearArray: true);
			}
		}

		public DependencyProperty Property { get; }

		public PropertyMetadata Metadata => _metadata ??= Property.GetMetadata(_dependencyObjectType);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="defaultValue">The default value of the Dependency Property</param>
		internal DependencyPropertyDetails(DependencyProperty property, Type dependencyObjectType, bool isTemplatedParentOrDataContext)
		{
			Property = property;
			_dependencyObjectType = dependencyObjectType;

			GetPropertyInheritanceConfiguration(isTemplatedParentOrDataContext, out var hasInherits, out var hasValueInherits, out var hasValueDoesNotInherits);

			_flags |= property.HasWeakStorage ? Flags.WeakStorage : Flags.None;
			_flags |= hasValueInherits ? Flags.ValueInherits : Flags.None;
			_flags |= hasValueDoesNotInherits ? Flags.ValueDoesNotInherit : Flags.None;
			_flags |= hasInherits ? Flags.Inherits : Flags.None;
		}

		internal static void SuppressLocalCanDefeatAnimations()
			=> _localCanDefeatAnimationSuppressed++;

		internal static void ContinueLocalCanDefeatAnimations()
			=> _localCanDefeatAnimationSuppressed--;

		private void GetPropertyInheritanceConfiguration(
			bool isTemplatedParentOrDataContext,
			out bool hasInherits,
			out bool hasValueInherits,
			out bool hasValueDoesNotInherit)
		{
			if (isTemplatedParentOrDataContext)
			{
				// TemplatedParent is a DependencyObject but does not propagate datacontext
				hasValueInherits = false;
				hasValueDoesNotInherit = true;
				hasInherits = true;
				return;
			}

			if (Metadata is FrameworkPropertyMetadata propertyMetadata)
			{
				hasValueInherits = propertyMetadata.Options.HasValueInheritsDataContext();
				hasValueDoesNotInherit = propertyMetadata.Options.HasValueDoesNotInheritDataContext();
				hasInherits = propertyMetadata.Options.HasInherits();
				return;
			}

			hasValueInherits = false;
			hasValueDoesNotInherit = false;
			hasInherits = false;
		}

		internal object? GetDefaultValue()
		{
			if (!HasDefaultValueSet)
			{
				_defaultValue = Metadata.DefaultValue;

				// Ensures that the default value of non-nullable properties is not null
				if (_defaultValue == null && !Property.IsTypeNullable)
				{
					_defaultValue = Property.GetFallbackDefaultValue();
				}

				_flags |= Flags.DefaultValueSet;
			}

			return _defaultValue;
		}

		/// <summary>
		/// Sets the value at the given level in the stack
		/// </summary>
		/// <param name="value">The value to set</param>
		/// <param name="precedence">The precedence level to set the value at</param>
		internal void SetValue(object? value, DependencyPropertyValuePrecedences precedence)
		{
			Property.ValidateValue(value);

			if (_localCanDefeatAnimationSuppressed == 0 &&
				precedence == DependencyPropertyValuePrecedences.Local &&
				_highestPrecedence == DependencyPropertyValuePrecedences.Animations &&
				value is not UnsetValue)
			{
				_flags |= Flags.LocalValueNewerThanAnimationsValue;
			}
			else
			{
				// This might not make much sense, but this is what we are seeing in WinUI code.
				// See https://github.com/unoplatform/uno/issues/5168#issuecomment-1948115761
				// If it turned out there is more complexity going on in WinUI, we can adjust as needed.
				_flags &= ~Flags.LocalValueNewerThanAnimationsValue;
			}

			if (!SetValueFast(value, precedence))
			{
				SetValueFull(value, precedence);
			}
		}

		private void SetValueFull(object? value, DependencyPropertyValuePrecedences precedence)
		{
			var valueIsUnsetValue = value == DependencyProperty.UnsetValue;

			var stackAlias = Stack;

			if (HasWeakStorage)
			{
				if (stackAlias[(int)precedence] is ManagedWeakReference mwr)
				{
					WeakReferencePool.ReturnWeakReference(this, mwr);
				}

				stackAlias[(int)precedence] = Validate(value);
			}
			else
			{
				stackAlias[(int)precedence] = ValidateNoWrap(value);
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
				// Start from current precedence and find next highest
				for (int i = (int)precedence; i < (int)DependencyPropertyValuePrecedences.DefaultValue; i++)
				{
					if (stackAlias[i] != DependencyProperty.UnsetValue)
					{
						_highestPrecedence = (DependencyPropertyValuePrecedences)i;
						return;
					}
				}

				_highestPrecedence = DependencyPropertyValuePrecedences.DefaultValue;
			}
		}

		private bool SetValueFast(object? value, DependencyPropertyValuePrecedences precedence)
		{
			if (_stack == null && precedence == DependencyPropertyValuePrecedences.Local)
			{
				var valueIsUnsetValue = value == DependencyProperty.UnsetValue;

				if (HasWeakStorage)
				{
					if (_fastLocalValue is ManagedWeakReference mwr2)
					{
						WeakReferencePool.ReturnWeakReference(this, mwr2);
					}

					_fastLocalValue = Validate(value);
				}
				else
				{
					_fastLocalValue = ValidateNoWrap(value);
				}

				_highestPrecedence = valueIsUnsetValue
					? DependencyPropertyValuePrecedences.DefaultValue
					: DependencyPropertyValuePrecedences.Local;

				return true;
			}

			return false;
		}

		internal void SetDefaultValue(object? defaultValue)
		{
			_defaultValue = defaultValue;
			_flags |= Flags.DefaultValueSet;
		}

		internal BindingExpression? GetBinding()
			=> _binding;

		internal void SetBinding(BindingExpression bindingExpression)
		{
			_binding = bindingExpression;
		}

		internal void ClearBinding()
		{
			_binding?.Dispose();
			_binding = null;
		}

		internal void SetSourceValue(object value) => _binding?.SetSourceValue(value);

		/// <summary>
		/// Gets the value at the current highest precedence level
		/// </summary>
		/// <returns>The value at the current highest precedence level</returns>
		internal object? GetValue()
			// Comment originates from WinUI source code (CModifiedValue::GetEffectiveValue)
			// If a local value has been set after an animated value, the local
			// value has precedence. This is different from WPF and is done because
			// some legacy SL apps depend on this and because SL Animation thinks that
			// it is better design for an animation in filling period to be trumped by a
			// local value. In the active period of an animation, the next animated
			// value will take precedence over the old local value.
			=> GetValue(_highestPrecedence == DependencyPropertyValuePrecedences.Animations && (_flags & Flags.LocalValueNewerThanAnimationsValue) != 0
				? DependencyPropertyValuePrecedences.Local
				: _highestPrecedence);

		/// <summary>
		/// Gets the value at a given precedence level
		/// </summary>
		/// <param name="precedence">The precedence level to get the value at</param>
		/// <returns>The value at a given precedence level</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal object? GetValue(DependencyPropertyValuePrecedences precedence)
		{
			if (_stack == null)
			{
				return precedence switch
				{
					DependencyPropertyValuePrecedences.DefaultValue => GetDefaultValue(),
					DependencyPropertyValuePrecedences.Local when _highestPrecedence == DependencyPropertyValuePrecedences.Local => Unwrap(_fastLocalValue),
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

				return Unwrap(Stack[(int)precedence]);
			}
		}

		/// <summary>
		/// Gets the value at the highest precedence level under the specified one
		/// E.G. If a property has a value both on the Animation, Local and Default
		/// precedences, and the given precedence is Animation, then the Local value is returned.
		/// </summary>
		/// <param name="precedence">The value precedence under which to fetch a value</param>
		/// <returns>The value at the highest precedence level under the specified one</returns>
		internal (object? value, DependencyPropertyValuePrecedences precedence) GetValueUnderPrecedence(DependencyPropertyValuePrecedences precedence)
		{
			if (_stack == null)
			{
				if (_highestPrecedence == DependencyPropertyValuePrecedences.Local)
				{
					return (Unwrap(_fastLocalValue), DependencyPropertyValuePrecedences.Local);
				}
				else
				{
					return (GetDefaultValue(), DependencyPropertyValuePrecedences.DefaultValue);
				}
			}
			else
			{
				var stackAlias = Stack;

				// Start from current precedence and find next highest
				for (int i = (int)precedence + 1; i < (int)DependencyPropertyValuePrecedences.DefaultValue; i++)
				{
					object? value = Unwrap(stackAlias[i]);

					if (value != DependencyProperty.UnsetValue)
					{
						return (value, (DependencyPropertyValuePrecedences)i);
					}
				}

				return (stackAlias[(int)DependencyPropertyValuePrecedences.DefaultValue], DependencyPropertyValuePrecedences.DefaultValue);
			}
		}

		/// <summary>
		/// Gets the current highest value precedence level
		/// </summary>
		internal DependencyPropertyValuePrecedences CurrentHighestValuePrecedence
			=> _highestPrecedence;

		/// <summary>
		/// Validate the value to prevent setting null to non-nullable dependency properties.
		/// </summary>
		/// <param name="value">value to validate</param>
		/// <returns>The value if valid, otherwise the dependency property's default value.</returns>
		private object? Validate(object? value)
		{
			return value == null && !Property.IsTypeNullable
				? GetDefaultValue()
				: Wrap(value);
		}

		/// <summary>
		/// Validate the value to prevent setting null to non-nullable dependency properties.
		/// </summary>
		/// <param name="value">value to validate</param>
		/// <returns>The value if valid, otherwise the dependency property's default value.</returns>
		private object? ValidateNoWrap(object? value)
		{
			return value == null && !Property.IsTypeNullable
				? GetDefaultValue()
				: value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private object? Wrap(object? value)
			=> HasWeakStorage && value != null && value != DependencyProperty.UnsetValue
			? WeakReferencePool.RentWeakReference(this, value)
			: value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private object? Unwrap(object? value)
			=> HasWeakStorage && value is ManagedWeakReference mwr
			? mwr.Target
			: value;

		private bool HasWeakStorage
			=> (_flags & Flags.WeakStorage) != 0;

		private bool HasDefaultValueSet
			=> (_flags & Flags.DefaultValueSet) != 0;

		internal bool HasValueInherits
			=> (_flags & Flags.ValueInherits) != 0;

		internal bool HasValueDoesNotInherit
			=> (_flags & Flags.ValueDoesNotInherit) != 0;

		internal bool HasInherits
			=> (_flags & Flags.Inherits) != 0;

		private object?[] Stack
		{
			get
			{
				if (_stack == null)
				{
					_stack = _pool.Rent(StackSize);

					MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_unsetStack), StackSize)
						.CopyTo(MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(_stack)!, StackSize));

					_stack[DefaultValueIndex] = GetDefaultValue();

					if (_highestPrecedence == DependencyPropertyValuePrecedences.Local)
					{
						_stack[(int)DependencyPropertyValuePrecedences.Local] = _fastLocalValue;
					}

					_fastLocalValue = null;
				}

				return _stack;
			}
		}

		#region IEnumerable implementation

		public IEnumerator<object?> GetEnumerator()
		{
			return Stack.Cast<object?>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return Stack.GetEnumerator();
		}

		#endregion

		public override string ToString()
		{
			return $"DependencyPropertyDetails({Property.Name})";
		}

		internal IDisposable RegisterCallback(PropertyChangedCallback callback)
			=> (_callbackManager ??= new DependencyPropertyCallbackManager()).RegisterCallback(callback);

		internal void RaisePropertyChanged(DependencyObject actualInstanceAlias, DependencyPropertyChangedEventArgs eventArgs)
			=> _callbackManager?.RaisePropertyChanged(actualInstanceAlias, eventArgs);

		[Flags]
		private enum Flags : byte
		{
			/// <summary>
			/// No flag is being set
			/// </summary>
			None = 0,

			/// <summary>
			/// This dependency property uses weak storage for its values
			/// </summary>
			WeakStorage = 1 << 0,

			/// <summary>
			/// Determines if the default value has been populated
			/// </summary>
			DefaultValueSet = 1 << 1,

			/// <summary>
			/// Determines if the property inherits DataContext from its parent
			/// </summary>
			ValueInherits = 1 << 2,

			/// <summary>
			/// Determines if the property must not inherit DataContext from its parent
			/// </summary>
			ValueDoesNotInherit = 1 << 3,

			/// <summary>
			/// Determines if the property inherits Value from its parent
			/// </summary>
			Inherits = 1 << 4,

			/// <summary>
			/// Normally, Animations has higher precedence than Local. However,
			/// we want local to take higher precedence if it's newer.
			/// This flag records this information.
			/// </summary>
			LocalValueNewerThanAnimationsValue = 1 << 5,
		}
	}
}
