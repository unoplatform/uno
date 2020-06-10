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
	internal class DependencyPropertyDetails : IEnumerable<object>, IEnumerable, IDisposable
	{
		private DependencyPropertyValuePrecedences _highestPrecedence = DependencyPropertyValuePrecedences.DefaultValue;
		private BindingExpression _lastBindings;
		private readonly static ArrayPool<object> _pool = ArrayPool<object>.Create(100, 100);
		private readonly object[] _stack;
		private readonly bool _hasWeakStorage;
		private readonly List<BindingExpression> _bindings = new List<BindingExpression>();

		private const int MaxIndex = (int)DependencyPropertyValuePrecedences.DefaultValue;

        private DependencyPropertyDetails()
		{
			_stack = _pool.Rent(MaxIndex + 1);
		}

		public void Dispose()
		{
			CallbackManager.Dispose();
			_pool.Return(_stack, clearArray: true);
		}

        public DependencyPropertyCallbackManager CallbackManager { get; } = new DependencyPropertyCallbackManager();

        public DependencyProperty Property { get; }

		public PropertyMetadata Metadata { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="defaultValue">The default value of the Dependency Property</param>
		internal DependencyPropertyDetails(DependencyProperty property, Type dependencyObjectType) : this()
		{
            Property = property;
			_hasWeakStorage = property.HasWeakStorage;

			for (int i = 0; i < MaxIndex; i++)
			{
				_stack[i] = DependencyProperty.UnsetValue;
			}

			var defaultValue = Property.GetMetadata(dependencyObjectType).DefaultValue;

			// Ensures that the default value of non-nullable properties is not null
			if (defaultValue == null && !Property.IsTypeNullable)
			{
				defaultValue = Property.GetFallbackDefaultValue();
			}

			_stack[MaxIndex] = defaultValue;

			Metadata = property.GetMetadata(dependencyObjectType);
		}

		/// <summary>
		/// Sets the value at the given level in the stack
		/// </summary>
		/// <param name="value">The value to set</param>
		/// <param name="precedence">The precedence level to set the value at</param>
		internal void SetValue(object value, DependencyPropertyValuePrecedences precedence)
		{
			if (_hasWeakStorage && _stack[(int)precedence] is ManagedWeakReference mwr)
			{
				WeakReferencePool.ReturnWeakReference(this, mwr);
			}

			_stack[(int)precedence] = Validate(value);

			// After setting the value, we need to update the current highest precedence if needed
			// If a higher value precedence was set, then this is the new highest
			if (!(value is UnsetValue) && precedence < _highestPrecedence)
			{
				_highestPrecedence = precedence;
				return;
			}

			// If we were unsetting the current highest precedence value, we need to find the next highest
			if (value is UnsetValue && precedence == _highestPrecedence)
			{
				// Start from current precedence and find next highest
				for (int i = (int)precedence; i < (int)DependencyPropertyValuePrecedences.DefaultValue; i++)
				{
					if (_stack[i] != DependencyProperty.UnsetValue)
					{
						_highestPrecedence = (DependencyPropertyValuePrecedences)i;
						return;
					}
				}

				_highestPrecedence = DependencyPropertyValuePrecedences.DefaultValue;
			}
		}

		internal BindingExpression GetLastBinding()
			=> _lastBindings;

		internal void SetBinding(BindingExpression bindingExpression)
		{
			_bindings.Add(bindingExpression);
			_lastBindings = bindingExpression;
		}

		internal void SetSourceValue(object value)
		{
			for (int i = 0; i < _bindings.Count; i++)
			{
				_bindings[i].SetSourceValue(value);
			}
		}

		/// <summary>
		/// Gets the value at the current highest precedence level
		/// </summary>
		/// <returns>The value at the current highest precedence level</returns>
		internal object GetValue()
			=> GetValue(_highestPrecedence);

		/// <summary>
		/// Gets the value at a given precedence level
		/// </summary>
		/// <param name="precedence">The precedence level to get the value at</param>
		/// <returns>The value at a given precedence level</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal object GetValue(DependencyPropertyValuePrecedences precedence)
			=> Unwrap(_stack[(int)precedence]);

		/// <summary>
		/// Gets the value at the highest precedence level under the specified one
		/// E.G. If a property has a value both on the Animation, Local and Default
		/// precedences, and the given precedence is Animation, then the Local value is returned.
		/// </summary>
		/// <param name="precedence">The value precedence under which to fetch a value</param>
		/// <returns>The value at the highest precedence level under the specified one</returns>
		internal (object value, DependencyPropertyValuePrecedences precedence) GetValueUnderPrecedence(DependencyPropertyValuePrecedences precedence)
		{
			// Start from current precedence and find next highest
			for (int i = (int)precedence + 1; i < (int)DependencyPropertyValuePrecedences.DefaultValue; i++)
			{
				object value = Unwrap(_stack[i]);

				if (value != DependencyProperty.UnsetValue)
				{
					return (value, (DependencyPropertyValuePrecedences)i);
				}
			}

			return (_stack[(int)DependencyPropertyValuePrecedences.DefaultValue], DependencyPropertyValuePrecedences.DefaultValue);
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
		private object Validate(object value)
		{
			return value == null && !Property.IsTypeNullable
				? _stack[(int)DependencyPropertyValuePrecedences.DefaultValue]
				: Wrap(value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private object Wrap(object value)
			=> _hasWeakStorage && value != null && value != DependencyProperty.UnsetValue
			? WeakReferencePool.RentWeakReference(this, value)
			: value;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private object Unwrap(object value)
			=> _hasWeakStorage && value is ManagedWeakReference mwr
			? mwr.Target
			: value;

		#region IEnumerable implementation

		public IEnumerator<object> GetEnumerator()
		{
			return _stack.Cast<object>().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _stack.GetEnumerator();
		}

		#endregion

		public override string ToString()
		{
			return $"DependencyPropertyDetails({Property.Name})";
		}
	}
}
