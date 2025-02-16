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
	/// Represents the the value for a given dependency property on a specific DependencyObject instance.
	/// </summary>
	/// <remarks>
	/// This class always stores the inherited value.
	/// For the rest of precedences, they are not all stored.
	/// NOTE: ModifiedValue means either coerced or animated.
	/// BaseValue means not coerced and not animated.
	/// _value could be an instance of ModifiedValue (which holds animated and coercion values, as well as the base value).
	/// Or if it's not ModifiedValue, then it's directly the base value.
	/// </remarks>
	internal class DependencyPropertyDetails : IDisposable
	{
		private DependencyPropertyValuePrecedences _baseValueSource = DependencyPropertyValuePrecedences.DefaultValue;
		private object? _value = DependencyProperty.UnsetValue;
		private object? _inheritedValue = DependencyProperty.UnsetValue;
		private BindingExpression? _binding;
		private Flags _flags;
		private DependencyPropertyCallbackManager? _callbackManager;

		internal void CloneToForHotReload(DependencyPropertyDetails other)
		{
			// If the old instance has a local value **and** the new instance doesn't, then copy the local value.
			// We shouldn't be copying local value if the new instance already has it set. The new value in the new instance
			// should not be overwritten as it's more likely to be more correct.
			if (this._baseValueSource == DependencyPropertyValuePrecedences.Local &&
				other._baseValueSource > DependencyPropertyValuePrecedences.Local)
			{
				other.SetValue(this.GetBaseValue(), DependencyPropertyValuePrecedences.Local);
			}
		}

		public void Dispose()
		{
			_callbackManager?.Dispose();
		}

		public DependencyProperty Property { get; }

		internal DependencyPropertyDetails(DependencyProperty property, bool isTemplatedParentOrDataContext)
		{
			Property = property;

			GetPropertyInheritanceConfiguration(property, isTemplatedParentOrDataContext, out var hasValueInherits, out var hasValueDoesNotInherits);

			_flags |= property.HasWeakStorage ? Flags.WeakStorage : Flags.None;
			_flags |= hasValueInherits ? Flags.ValueInherits : Flags.None;
			_flags |= hasValueDoesNotInherits ? Flags.ValueDoesNotInherit : Flags.None;
		}

		private void GetPropertyInheritanceConfiguration(
			DependencyProperty property,
			bool isTemplatedParentOrDataContext,
			out bool hasValueInherits,
			out bool hasValueDoesNotInherit)
		{
			if (isTemplatedParentOrDataContext)
			{
				// TemplatedParent is a DependencyObject but does not propagate datacontext
				hasValueInherits = false;
				hasValueDoesNotInherit = true;
				return;
			}

			if (property.Metadata is FrameworkPropertyMetadata propertyMetadata)
			{
				hasValueInherits = propertyMetadata.Options.HasValueInheritsDataContext();
				hasValueDoesNotInherit = propertyMetadata.Options.HasValueDoesNotInheritDataContext();
				return;
			}

			hasValueInherits = false;
			hasValueDoesNotInherit = false;
		}

		private ModifiedValue EnsureModifiedValue()
		{
			// If we already have ModifiedValue, then return it.
			if (_value is ModifiedValue modifiedValue)
			{
				return modifiedValue;
			}

			// Otherwise, create a new ModifiedValue, and move the BaseValue to it.
			modifiedValue = new ModifiedValue();
			modifiedValue.SetBaseValue(GetBaseValue(), _baseValueSource);
			_value = modifiedValue;
			return modifiedValue;
		}

		private void SetBaseValue(object? value, DependencyPropertyValuePrecedences precedence)
		{
			// Always set inherited value.
			// This is needed for now to always be able to restore inherited value efficiently when higher precedences are cleared.
			if (precedence == DependencyPropertyValuePrecedences.Inheritance)
			{
				_inheritedValue = value;
			}

			// We are setting or unsetting a value for precedence lower than our current.
			// We need to do nothing.
			if (precedence > _baseValueSource)
			{
				return;
			}

			if (value != DependencyProperty.UnsetValue)
			{
				// If we are setting a value with precedence higher than our current. Then update the current precedence.
				_baseValueSource = precedence;
			}
			else
			{
				// If we are unsetting the current highest precedence, then
				// update the highest precedence to either DefaultValue or Inheritance depending on whether we have an inherited value.
				// Caller will re-evaluate base value. For example, if we are unsetting Local value, the base value could be a style.
				// It's the caller responsibility to evaluate styles and set the base value again.
				if (_baseValueSource == precedence)
				{
					// Caller will re-evaluate base value.
					_baseValueSource = _inheritedValue == DependencyProperty.UnsetValue
						? DependencyPropertyValuePrecedences.DefaultValue
						: DependencyPropertyValuePrecedences.Inheritance;
				}
			}

			if (_value is ModifiedValue modifiedValue)
			{
				// If our value is ModifiedValue, then the BaseValue is stored there.
				modifiedValue.SetBaseValue(value, precedence);

				if (_baseValueSource == DependencyPropertyValuePrecedences.Inheritance)
				{
					modifiedValue.SetBaseValue(_inheritedValue, DependencyPropertyValuePrecedences.Inheritance);
				}
			}
			else
			{
				// Otherwise, the BaseValue is stored directly in the _value field.
				_value = _baseValueSource == DependencyPropertyValuePrecedences.Inheritance ? _inheritedValue : value;
			}
		}

		/// <summary>
		/// Sets the value at the given level in the stack
		/// </summary>
		/// <param name="value">The value to set</param>
		/// <param name="precedence">The precedence level to set the value at</param>
		internal void SetValue(object? value, DependencyPropertyValuePrecedences precedence)
		{
			Property.ValidateValue(value);

			if (HasWeakStorage)
			{
				value = Validate(value);
			}
			else
			{
				value = ValidateNoWrap(value);
			}

			switch (precedence)
			{
				case DependencyPropertyValuePrecedences.Coercion:
					EnsureModifiedValue().CoercedValue = value;
					break;

				case DependencyPropertyValuePrecedences.Animations:
					EnsureModifiedValue().SetAnimatedValue(value);
					break;

				default:
					SetBaseValue(value, precedence);
					break;
			}
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
		internal object? GetEffectiveValue()
			=> Unwrap(_value is ModifiedValue modifiedValue ? modifiedValue.GetEffectiveValue() : _value);

		internal object? GetBaseValue()
			=> Unwrap(_value is ModifiedValue modifiedValue ? modifiedValue.GetBaseValue() : _value);

		internal DependencyPropertyValuePrecedences GetBaseValueSource()
			=> _baseValueSource;

		internal object? GetInheritedValue()
			=> _inheritedValue;

		internal ModifiedValue? GetModifiedValue()
			=> _value as ModifiedValue;

		/// <summary>
		/// Gets the current highest value precedence level
		/// </summary>
		internal DependencyPropertyValuePrecedences CurrentHighestValuePrecedence
		{
			get
			{
				if (_value is ModifiedValue modifiedValue)
				{
					if (modifiedValue.IsCoerced)
					{
						return DependencyPropertyValuePrecedences.Coercion;
					}
					else if (modifiedValue.IsAnimated && !modifiedValue.LocalValueNewerThanAnimatedValue)
					{
						return DependencyPropertyValuePrecedences.Animations;
					}

					return _baseValueSource;
				}
				else
				{
					return _baseValueSource;
				}
			}
		}

		/// <summary>
		/// Validate the value to prevent setting null to non-nullable dependency properties.
		/// </summary>
		/// <param name="value">value to validate</param>
		/// <returns>The value if valid, otherwise the dependency property's default value.</returns>
		private object? Validate(object? value)
		{
			return value == null && !Property.IsTypeNullable
				? throw new InvalidOperationException("DP cannot be set to null when its type is not nullable.")
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
				? throw new InvalidOperationException("DP cannot be set to null when its type is not nullable.")
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

		internal bool HasValueInherits
			=> (_flags & Flags.ValueInherits) != 0;

		internal bool HasValueDoesNotInherit
			=> (_flags & Flags.ValueDoesNotInherit) != 0;

		public override string ToString()
		{
			return $"DependencyPropertyDetails({Property.Name})";
		}

		internal IDisposable RegisterCallback(PropertyChangedCallback callback)
			=> (_callbackManager ??= new DependencyPropertyCallbackManager()).RegisterCallback(callback);

		internal bool CanRaisePropertyChanged => _callbackManager is not null;

		internal void RaisePropertyChangedNoNullCheck(DependencyObject actualInstanceAlias, DependencyPropertyChangedEventArgs eventArgs)
			=> _callbackManager!.RaisePropertyChanged(actualInstanceAlias, eventArgs);

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
			/// Determines if the property inherits DataContext from its parent
			/// </summary>
			ValueInherits = 1 << 1,

			/// <summary>
			/// Determines if the property must not inherit DataContext from its parent
			/// </summary>
			ValueDoesNotInherit = 1 << 2,
		}
	}
}
