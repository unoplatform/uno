#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using Uno.Foundation.Logging;
using Windows.Foundation.Metadata;
using Windows.UI;
using Windows.UI.Core;

namespace Microsoft.UI.Composition
{
	public partial class CompositionObject : IDisposable
	{
		private readonly ContextStore _contextStore = new ContextStore();
		private CompositionPropertySet? _properties;
		private Dictionary<string, CompositionAnimation>? _animations;

		internal CompositionObject()
		{
			ApiInformation.TryRaiseNotImplemented(GetType().FullName!, "The compositor constructor is not available, as the type is not implemented");
			Compositor = new Compositor();
		}

		internal CompositionObject(Compositor compositor)
		{
			Compositor = compositor;
		}

		public CompositionPropertySet Properties => _properties ??= GetProperties();

		public Compositor Compositor { get; }

		public CoreDispatcher Dispatcher => CoreDispatcher.Main;

		public string? Comment { get; set; }

		private CompositionPropertySet GetProperties()
		{
			if (this is CompositionPropertySet @this)
			{
				return @this;
			}

			return new CompositionPropertySet(Compositor);
		}

		private protected T ValidateValue<T>(object? value)
		{
			if (value is not T t)
			{
				if (Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture) is T changed)
				{
					return changed;
				}

				throw new ArgumentException($"Cannot convert value of type '{value?.GetType()}' to {typeof(T)}");
			}

			return t;
		}

		private bool IsDefinedInProperties(ReadOnlySpan<char> propertyName)
		{
			// Note: We don't care about the parameter type. So, anything can be used (here we are using bool).
			// It only decides whether the return status is Succeeded or TypeMismatch, which are equally treated as we are looking for
			// anything other than NotFound.
			return _properties is not null && _properties.TryGetValue<bool>(propertyName.ToString(), out _) != CompositionGetValueStatus.NotFound;
		}

		private protected Color GetColor(ReadOnlySpan<char> subPropertyName, Color existingValue, object? propertyValue)
		{
			if (subPropertyName.Length == 0)
			{
				return ValidateValue<Color>(propertyValue);
			}

			if (subPropertyName.Equals("A", StringComparison.Ordinal))
			{
				existingValue.A = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("R", StringComparison.Ordinal))
			{
				existingValue.R = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("G", StringComparison.Ordinal))
			{
				existingValue.G = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("B", StringComparison.Ordinal))
			{
				existingValue.B = ValidateValue<byte>(propertyValue);
			}
			else
			{
				throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
			}

			return existingValue;
		}

		private protected Matrix3x2 GetMatrix3x2(ReadOnlySpan<char> subPropertyName, Matrix3x2 existingValue, object? propertyValue)
		{
			if (subPropertyName.Length == 0)
			{
				return ValidateValue<Matrix3x2>(propertyValue);
			}

			if (subPropertyName.Equals("M11", StringComparison.Ordinal))
			{
				existingValue.M11 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M12", StringComparison.Ordinal))
			{
				existingValue.M12 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M21", StringComparison.Ordinal))
			{
				existingValue.M21 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M22", StringComparison.Ordinal))
			{
				existingValue.M22 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M31", StringComparison.Ordinal))
			{
				existingValue.M31 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M32", StringComparison.Ordinal))
			{
				existingValue.M32 = ValidateValue<float>(propertyValue);
			}
			else
			{
				throw new Exception($"Cannot update Matrix3x2 with a sub-property named '{subPropertyName}'.");
			}

			return existingValue;
		}

		private protected Matrix4x4 GetMatrix4x4(ReadOnlySpan<char> subPropertyName, Matrix4x4 existingValue, object? propertyValue)
		{
			if (subPropertyName.Length == 0)
			{
				return ValidateValue<Matrix4x4>(propertyValue);
			}

			if (subPropertyName.Equals("M11", StringComparison.Ordinal))
			{
				existingValue.M11 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M12", StringComparison.Ordinal))
			{
				existingValue.M12 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M13", StringComparison.Ordinal))
			{
				existingValue.M13 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M14", StringComparison.Ordinal))
			{
				existingValue.M14 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M21", StringComparison.Ordinal))
			{
				existingValue.M21 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M22", StringComparison.Ordinal))
			{
				existingValue.M22 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M23", StringComparison.Ordinal))
			{
				existingValue.M23 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M24", StringComparison.Ordinal))
			{
				existingValue.M24 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M31", StringComparison.Ordinal))
			{
				existingValue.M31 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M32", StringComparison.Ordinal))
			{
				existingValue.M32 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M33", StringComparison.Ordinal))
			{
				existingValue.M33 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M34", StringComparison.Ordinal))
			{
				existingValue.M34 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M41", StringComparison.Ordinal))
			{
				existingValue.M41 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M42", StringComparison.Ordinal))
			{
				existingValue.M42 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M43", StringComparison.Ordinal))
			{
				existingValue.M43 = ValidateValue<float>(propertyValue);
			}
			else if (subPropertyName.Equals("M44", StringComparison.Ordinal))
			{
				existingValue.M44 = ValidateValue<float>(propertyValue);
			}
			else
			{
				throw new Exception($"Cannot update Matrix3x2 with a sub-property named '{subPropertyName}'.");
			}

			return existingValue;
		}

		private protected Quaternion GetQuaternion(ReadOnlySpan<char> subPropertyName, Quaternion existingValue, object? propertyValue)
		{
			if (subPropertyName.Length == 0)
			{
				return ValidateValue<Quaternion>(propertyValue);
			}

			if (subPropertyName.Equals("X", StringComparison.Ordinal))
			{
				existingValue.X = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
			{
				existingValue.Y = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("Z", StringComparison.Ordinal))
			{
				existingValue.Z = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("W", StringComparison.Ordinal))
			{
				existingValue.W = ValidateValue<byte>(propertyValue);
			}
			else
			{
				throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
			}

			return existingValue;
		}

		private protected Vector2 GetVector2(ReadOnlySpan<char> subPropertyName, Vector2 existingValue, object? propertyValue)
		{
			if (subPropertyName.Length == 0)
			{
				return ValidateValue<Vector2>(propertyValue);
			}

			if (subPropertyName.Equals("X", StringComparison.Ordinal))
			{
				existingValue.X = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
			{
				existingValue.Y = ValidateValue<byte>(propertyValue);
			}
			else
			{
				throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
			}

			return existingValue;
		}

		private protected Vector3 GetVector3(ReadOnlySpan<char> subPropertyName, Vector3 existingValue, object? propertyValue)
		{
			if (subPropertyName.Length == 0)
			{
				return ValidateValue<Vector3>(propertyValue);
			}

			if (subPropertyName.Equals("X", StringComparison.Ordinal))
			{
				existingValue.X = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
			{
				existingValue.Y = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("Z", StringComparison.Ordinal))
			{
				existingValue.Z = ValidateValue<byte>(propertyValue);
			}
			else
			{
				throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
			}

			return existingValue;
		}

		private protected Vector4 GetVector4(ReadOnlySpan<char> subPropertyName, Vector4 existingValue, object? propertyValue)
		{
			if (subPropertyName.Length == 0)
			{
				return ValidateValue<Vector4>(propertyValue);
			}

			if (subPropertyName.Equals("X", StringComparison.Ordinal))
			{
				existingValue.X = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
			{
				existingValue.Y = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("Z", StringComparison.Ordinal))
			{
				existingValue.Z = ValidateValue<byte>(propertyValue);
			}
			else if (subPropertyName.Equals("W", StringComparison.Ordinal))
			{
				existingValue.W = ValidateValue<byte>(propertyValue);
			}
			else
			{
				throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
			}

			return existingValue;
		}

		// Overrides are based on:
		// https://learn.microsoft.com/en-us/uwp/api/windows.ui.composition.compositionobject.startanimation?view=winrt-22621
		private protected virtual bool IsAnimatableProperty(ReadOnlySpan<char> propertyName)
			=> IsDefinedInProperties(propertyName);

		private protected virtual void SetAnimatableProperty(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
		{
			if (_properties is not null)
			{
				var propertyNameString = propertyName.ToString();
				if (_properties.TryGetBoolean(propertyNameString, out _) == CompositionGetValueStatus.Succeeded)
				{
					_properties.InsertBoolean(propertyNameString, ValidateValue<bool>(propertyValue));
				}
				else if (_properties.TryGetColor(propertyNameString, out var color) == CompositionGetValueStatus.Succeeded)
				{
					_properties.InsertColor(propertyNameString, GetColor(subPropertyName, color, propertyValue));
				}
				else if (_properties.TryGetMatrix3x2(propertyNameString, out var matrix3x2) == CompositionGetValueStatus.Succeeded)
				{
					_properties.InsertMatrix3x2(propertyNameString, GetMatrix3x2(subPropertyName, matrix3x2, propertyValue));
				}
				else if (_properties.TryGetMatrix4x4(propertyNameString, out var matrix4x4) == CompositionGetValueStatus.Succeeded)
				{
					_properties.InsertMatrix4x4(propertyNameString, GetMatrix4x4(subPropertyName, matrix4x4, propertyValue));
				}
				else if (_properties.TryGetQuaternion(propertyNameString, out var quaternion) == CompositionGetValueStatus.Succeeded)
				{
					_properties.InsertQuaternion(propertyNameString, GetQuaternion(subPropertyName, quaternion, propertyValue));
				}
				else if (_properties.TryGetScalar(propertyNameString, out _) == CompositionGetValueStatus.Succeeded)
				{
					_properties.InsertScalar(propertyNameString, ValidateValue<float>(propertyValue));
				}
				else if (_properties.TryGetVector2(propertyNameString, out var vector2) == CompositionGetValueStatus.Succeeded)
				{
					_properties.InsertVector2(propertyNameString, GetVector2(subPropertyName, vector2, propertyValue));
				}
				else if (_properties.TryGetVector3(propertyNameString, out var vector3) == CompositionGetValueStatus.Succeeded)
				{
					_properties.InsertVector3(propertyNameString, GetVector3(subPropertyName, vector3, propertyValue));
				}
				else if (_properties.TryGetVector4(propertyNameString, out var vector4) == CompositionGetValueStatus.Succeeded)
				{
					_properties.InsertVector4(propertyNameString, GetVector4(subPropertyName, vector4, propertyValue));
				}
				else
				{
					throw new Exception($"Unable to set property '{propertyName}' on {this}");
				}
			}
			else
			{
				throw new Exception($"Unable to set property '{propertyName}' on {this}");
			}
		}

		public void StartAnimation(string propertyName, CompositionAnimation animation)
		{
			ReadOnlySpan<char> firstPropertyName;
			ReadOnlySpan<char> subPropertyName;
			var firstDotIndex = propertyName.IndexOf('.');
			if (firstDotIndex > -1)
			{
				firstPropertyName = propertyName.AsSpan().Slice(0, firstDotIndex);
				subPropertyName = propertyName.AsSpan().Slice(firstDotIndex + 1);
			}
			else
			{
				firstPropertyName = propertyName;
				subPropertyName = default;
			}

			if (!IsAnimatableProperty(firstPropertyName))
			{
				throw new ArgumentException($"Property '{firstPropertyName}' is not animatable.");
			}

			if (_animations?.ContainsKey(propertyName) == true)
			{
				StopAnimation(propertyName);
			}

			_animations ??= new();
			_animations[propertyName] = animation;
			animation.PropertyChanged += ReEvaluateAnimation;
			var animationValue = animation.Start();

			try
			{
				this.SetAnimatableProperty(firstPropertyName, subPropertyName, animationValue);
			}
			catch (Exception ex)
			{
				// Important to catch the exception.
				// It can currently happen for non-implemented animations which will evaluate to null and the target animation property is value type.
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"An exception occurred while setting animation value '{animationValue}' to property '{propertyName}' for animation '{animation}'. {ex.Message}");
				}
			}
		}

		private void ReEvaluateAnimation(CompositionAnimation animation)
		{
			if (_animations == null)
			{
				return;
			}

			foreach (var (key, value) in _animations)
			{
				if (value == animation)
				{
					var propertyName = key;
					ReadOnlySpan<char> firstPropertyName;
					ReadOnlySpan<char> subPropertyName;
					var firstDotIndex = propertyName.IndexOf('.');
					if (firstDotIndex > -1)
					{
						firstPropertyName = propertyName.AsSpan().Slice(0, firstDotIndex);
						subPropertyName = propertyName.AsSpan().Slice(firstDotIndex + 1);
					}
					else
					{
						firstPropertyName = propertyName;
						subPropertyName = default;
					}

					this.SetAnimatableProperty(firstPropertyName, subPropertyName, animation.Evaluate());
				}
			}
		}

		public void StopAnimation(string propertyName)
		{
			if (_animations?.TryGetValue(propertyName, out var animation) == true)
			{
				animation.PropertyChanged -= ReEvaluateAnimation;
				animation.Stop();
				_animations.Remove(propertyName);
			}
		}

		public void Dispose() => DisposeInternal();

		private protected virtual void DisposeInternal()
		{
		}

		internal virtual void StartAnimationCore(string propertyName, CompositionAnimation animation)
		{

		}

		internal void AddContext(CompositionObject context, string? propertyName)
		{
			_contextStore.AddContext(context, propertyName);
		}

		internal void RemoveContext(CompositionObject context, string? propertyName)
		{
			_contextStore.RemoveContext(context, propertyName);
		}

		private protected void SetProperty(ref bool field, bool value, [CallerMemberName] string? propertyName = null)
		{
			if (field == value)
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref int field, int value, [CallerMemberName] string? propertyName = null)
		{
			if (field == value)
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref float field, float value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Matrix3x2 field, Matrix3x2 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Matrix4x4 field, Matrix4x4 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Vector2 field, Vector2 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Vector3 field, Vector3 value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Quaternion field, Quaternion value, [CallerMemberName] string? propertyName = null)
		{
			if (field.Equals(value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty(ref Color field, Color value, [CallerMemberName] string? propertyName = null)
		{
			if (field == value)
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetEnumProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
			where T : Enum
		{
			if (EqualityComparer<T>.Default.Equals(field, value))
			{
				return;
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
			where T : CompositionObject?
		{
			if (field == value)
			{
				return;
			}

			OnCompositionPropertyChanged(field, value, propertyName);

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void SetObjectProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
		{
			if (field?.Equals(value) ?? value == null)
			{
				return;
			}

			// This check is here for backward compatibility
			// Is this valid even for non-composition objects like interface?
			var fieldCO = field as CompositionObject;
			var valueCO = value as CompositionObject;
			if (fieldCO != null || value != null)
			{
				OnCompositionPropertyChanged(fieldCO, valueCO, propertyName);
			}

			field = value;

			OnPropertyChanged(propertyName, false);
		}

		private protected void OnChanged() => OnPropertyChanged(null, false);

		private protected void OnCompositionPropertyChanged(CompositionObject? oldValue, CompositionObject? newValue) => OnCompositionPropertyChanged(oldValue, newValue, null);

		private protected void OnCompositionPropertyChanged(CompositionObject? oldValue, CompositionObject? newValue, string? propertyName)
		{
			if (oldValue != null)
			{
				oldValue.RemoveContext(this, propertyName);
			}

			if (newValue != null)
			{
				newValue.AddContext(this, propertyName);
			}
		}

		private protected void OnPropertyChanged(string? propertyName, bool isSubPropertyChange)
		{
			OnPropertyChangedCore(propertyName, isSubPropertyChange);
			_contextStore.RaiseChanged();
		}

		private protected virtual void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
		}
	}
}
