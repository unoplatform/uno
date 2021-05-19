#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
		private BindingExpression? _lastBindings;
		private static readonly ArrayPool<object?> _pool = ArrayPool<object?>.Create(_stackLength, 100);
		private object?[]? _stack;
		private PropertyMetadata? _metadata;
		private object? _defaultValue;
		private List<BindingExpression>? _bindings;
		private Flags _flags;
		private DependencyPropertyCallbackManager? _callbackManager;

		private const int MaxIndex = (int)DependencyPropertyValuePrecedences.DefaultValue;
		private const int _stackLength = MaxIndex + 1;
		private static readonly object[] _unsetStack;
		static DependencyPropertyDetails()
		{
			_unsetStack = new object[_stackLength];
			for (var i = 0; i < _stackLength; i++)
			{
				_unsetStack[i] = DependencyProperty.UnsetValue;
			}
		}

		private List<BindingExpression> Bindings => _bindings ??= new List<BindingExpression>();

		private bool HasBindingsList => _bindings != null;

		public void Dispose()
		{
			CallbackManager.Dispose();

			if (_stack != null)
			{
				// Note that `clearArray` is required here to avoid pooled arrays to leak
				// instances from set property values.
				_pool.Return(_stack, clearArray: true);
			}
		}

        public DependencyPropertyCallbackManager CallbackManager => _callbackManager ??= new DependencyPropertyCallbackManager();

        public DependencyProperty Property { get; }

		public PropertyMetadata Metadata => _metadata ??= Property.GetMetadata(_dependencyObjectType);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="defaultValue">The default value of the Dependency Property</param>
		internal DependencyPropertyDetails(DependencyProperty property, Type dependencyObjectType)
		{
			Property = property;
			_dependencyObjectType = dependencyObjectType;

			if (property.HasWeakStorage)
			{
				_flags |= Flags.WeakStorage;
			}
		}

		private object? GetDefaultValue()
		{
			if (!HasDefaultValueSet)
			{
				_defaultValue = Property.GetMetadata(_dependencyObjectType).DefaultValue;

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
			if (!SetValueFast(value, precedence))
			{
				SetValueFull(value, precedence);
			}
		}

		private void SetValueFull(object? value, DependencyPropertyValuePrecedences precedence)
		{
			var valueIsUnsetValue = value is UnsetValue;

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
				var valueIsUnsetValue = value is UnsetValue;

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

		internal void SetDefaultValue(object defaultValue)
		{
			_defaultValue = defaultValue;
			_flags |= Flags.DefaultValueSet;
		}

		internal BindingExpression? GetLastBinding()
			=> _lastBindings;

		internal void SetBinding(BindingExpression bindingExpression)
		{
			Bindings.Add(bindingExpression);
			_lastBindings = bindingExpression;
		}

		internal void SetSourceValue(object value)
		{
			if (HasBindingsList)
			{
				for (int i = 0; i < Bindings.Count; i++)
				{
					Bindings[i].SetSourceValue(value);
				}
			}
		}

		/// <summary>
		/// Gets the value at the current highest precedence level
		/// </summary>
		/// <returns>The value at the current highest precedence level</returns>
		internal object? GetValue()
			=> GetValue(_highestPrecedence);

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
				if(_highestPrecedence == DependencyPropertyValuePrecedences.Local)
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

		private object?[] Stack
		{
			get
			{
				if (_stack == null)
				{
					_stack = _pool.Rent(_stackLength);

					Array.Copy(_unsetStack, _stack, _stackLength);

					var defaultValue = GetDefaultValue();

					_stack[MaxIndex] = defaultValue;

					if (_highestPrecedence == DependencyPropertyValuePrecedences.Local)
					{
						_stack[(int)DependencyPropertyValuePrecedences.Local] = _fastLocalValue;
					}
				}

				return _stack;
			}
		}

		#region IEnumerable implementation

		public IEnumerator<object?> GetEnumerator()
		{
			return _stack.Cast<object?>().GetEnumerator();
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

		[Flags]
		enum Flags
		{
			/// <summary>
			/// This dependency property uses weak storage for its values
			/// </summary>
			WeakStorage = 1 << 0,

			/// <summary>
			/// Determines if the default value has been populated
			/// </summary>
			DefaultValueSet = 1 << 1,
		}
	}
}
