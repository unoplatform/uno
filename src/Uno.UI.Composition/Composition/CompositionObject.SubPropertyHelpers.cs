#nullable enable

using System;
using System.Numerics;
using Windows.UI;

namespace Microsoft.UI.Composition;

partial class CompositionObject
{
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

	private void TryUpdateFromProperties(ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
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
}
