#nullable enable

using System;
using System.Numerics;
using Windows.UI;

namespace Microsoft.UI.Composition;

partial class CompositionObject
{
	private protected Color UpdateColor(ReadOnlySpan<char> subPropertyName, Color existingValue, object? propertyValue)
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

	private protected Matrix3x2 UpdateMatrix3x2(ReadOnlySpan<char> subPropertyName, Matrix3x2 existingValue, object? propertyValue)
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

	private protected Matrix4x4 UpdateMatrix4x4(ReadOnlySpan<char> subPropertyName, Matrix4x4 existingValue, object? propertyValue)
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

	private protected Quaternion UpdateQuaternion(ReadOnlySpan<char> subPropertyName, Quaternion existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Quaternion>(propertyValue);
		}

		if (subPropertyName.Equals("X", StringComparison.Ordinal))
		{
			existingValue.X = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
		{
			existingValue.Y = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Z", StringComparison.Ordinal))
		{
			existingValue.Z = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("W", StringComparison.Ordinal))
		{
			existingValue.W = ValidateValue<float>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	private protected Vector2 UpdateVector2(ReadOnlySpan<char> subPropertyName, Vector2 existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Vector2>(propertyValue);
		}

		if (subPropertyName.Equals("X", StringComparison.Ordinal))
		{
			existingValue.X = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
		{
			existingValue.Y = ValidateValue<float>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	private protected Vector3 UpdateVector3(ReadOnlySpan<char> subPropertyName, Vector3 existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Vector3>(propertyValue);
		}

		if (subPropertyName.Equals("X", StringComparison.Ordinal))
		{
			existingValue.X = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
		{
			existingValue.Y = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Z", StringComparison.Ordinal))
		{
			existingValue.Z = ValidateValue<float>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	private protected Vector4 UpdateVector4(ReadOnlySpan<char> subPropertyName, Vector4 existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Vector4>(propertyValue);
		}

		if (subPropertyName.Equals("X", StringComparison.Ordinal))
		{
			existingValue.X = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
		{
			existingValue.Y = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Z", StringComparison.Ordinal))
		{
			existingValue.Z = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("W", StringComparison.Ordinal))
		{
			existingValue.W = ValidateValue<float>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	private protected object GetColor(string subPropertyName, Color existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("A", StringComparison.Ordinal))
		{
			return existingValue.A;
		}
		else if (subPropertyName.Equals("R", StringComparison.Ordinal))
		{
			return existingValue.R;
		}
		else if (subPropertyName.Equals("G", StringComparison.Ordinal))
		{
			return existingValue.G;
		}
		else if (subPropertyName.Equals("B", StringComparison.Ordinal))
		{
			return existingValue.B;
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}
	}

	private protected object GetMatrix3x2(string subPropertyName, Matrix3x2 existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("M11", StringComparison.Ordinal))
		{
			return existingValue.M11;
		}
		else if (subPropertyName.Equals("M12", StringComparison.Ordinal))
		{
			return existingValue.M12;
		}
		else if (subPropertyName.Equals("M21", StringComparison.Ordinal))
		{
			return existingValue.M21;
		}
		else if (subPropertyName.Equals("M22", StringComparison.Ordinal))
		{
			return existingValue.M22;
		}
		else if (subPropertyName.Equals("M31", StringComparison.Ordinal))
		{
			return existingValue.M31;
		}
		else if (subPropertyName.Equals("M32", StringComparison.Ordinal))
		{
			return existingValue.M32;
		}
		else
		{
			throw new Exception($"Cannot update Matrix3x2 with a sub-property named '{subPropertyName}'.");
		}
	}

	private protected object GetMatrix4x4(string subPropertyName, Matrix4x4 existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("M11", StringComparison.Ordinal))
		{
			return existingValue.M11;
		}
		else if (subPropertyName.Equals("M12", StringComparison.Ordinal))
		{
			return existingValue.M12;
		}
		else if (subPropertyName.Equals("M13", StringComparison.Ordinal))
		{
			return existingValue.M13;
		}
		else if (subPropertyName.Equals("M14", StringComparison.Ordinal))
		{
			return existingValue.M14;
		}
		else if (subPropertyName.Equals("M21", StringComparison.Ordinal))
		{
			return existingValue.M21;
		}
		else if (subPropertyName.Equals("M22", StringComparison.Ordinal))
		{
			return existingValue.M22;
		}
		else if (subPropertyName.Equals("M23", StringComparison.Ordinal))
		{
			return existingValue.M23;
		}
		else if (subPropertyName.Equals("M24", StringComparison.Ordinal))
		{
			return existingValue.M24;
		}
		else if (subPropertyName.Equals("M31", StringComparison.Ordinal))
		{
			return existingValue.M31;
		}
		else if (subPropertyName.Equals("M32", StringComparison.Ordinal))
		{
			return existingValue.M32;
		}
		else if (subPropertyName.Equals("M33", StringComparison.Ordinal))
		{
			return existingValue.M33;
		}
		else if (subPropertyName.Equals("M34", StringComparison.Ordinal))
		{
			return existingValue.M34;
		}
		else if (subPropertyName.Equals("M41", StringComparison.Ordinal))
		{
			return existingValue.M41;
		}
		else if (subPropertyName.Equals("M42", StringComparison.Ordinal))
		{
			return existingValue.M42;
		}
		else if (subPropertyName.Equals("M43", StringComparison.Ordinal))
		{
			return existingValue.M43;
		}
		else if (subPropertyName.Equals("M44", StringComparison.Ordinal))
		{
			return existingValue.M44;
		}
		else
		{
			throw new Exception($"Cannot update Matrix3x2 with a sub-property named '{subPropertyName}'.");
		}
	}

	private protected object GetQuaternion(string subPropertyName, Quaternion existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("X", StringComparison.Ordinal))
		{
			return existingValue.X;
		}
		else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
		{
			return existingValue.Y;
		}
		else if (subPropertyName.Equals("Z", StringComparison.Ordinal))
		{
			return existingValue.Z;
		}
		else if (subPropertyName.Equals("W", StringComparison.Ordinal))
		{
			return existingValue.W;
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}
	}

	private protected object GetVector2(string subPropertyName, Vector2 existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("X", StringComparison.Ordinal))
		{
			return existingValue.X;
		}
		else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
		{
			return existingValue.Y;
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}
	}

	private protected object GetVector3(string subPropertyName, Vector3 existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("X", StringComparison.Ordinal))
		{
			return existingValue.X;
		}
		else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
		{
			return existingValue.Y;
		}
		else if (subPropertyName.Equals("Z", StringComparison.Ordinal))
		{
			return existingValue.Z;
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}
	}

	private protected object GetVector4(string subPropertyName, Vector4 existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("X", StringComparison.Ordinal))
		{
			return existingValue.X;
		}
		else if (subPropertyName.Equals("Y", StringComparison.Ordinal))
		{
			return existingValue.Y;
		}
		else if (subPropertyName.Equals("Z", StringComparison.Ordinal))
		{
			return existingValue.Z;
		}
		else if (subPropertyName.Equals("W", StringComparison.Ordinal))
		{
			return existingValue.W;
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}
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
				_properties.InsertColor(propertyNameString, UpdateColor(subPropertyName, color, propertyValue));
			}
			else if (_properties.TryGetMatrix3x2(propertyNameString, out var matrix3x2) == CompositionGetValueStatus.Succeeded)
			{
				_properties.InsertMatrix3x2(propertyNameString, UpdateMatrix3x2(subPropertyName, matrix3x2, propertyValue));
			}
			else if (_properties.TryGetMatrix4x4(propertyNameString, out var matrix4x4) == CompositionGetValueStatus.Succeeded)
			{
				_properties.InsertMatrix4x4(propertyNameString, UpdateMatrix4x4(subPropertyName, matrix4x4, propertyValue));
			}
			else if (_properties.TryGetQuaternion(propertyNameString, out var quaternion) == CompositionGetValueStatus.Succeeded)
			{
				_properties.InsertQuaternion(propertyNameString, UpdateQuaternion(subPropertyName, quaternion, propertyValue));
			}
			else if (_properties.TryGetScalar(propertyNameString, out _) == CompositionGetValueStatus.Succeeded)
			{
				_properties.InsertScalar(propertyNameString, ValidateValue<float>(propertyValue));
			}
			else if (_properties.TryGetVector2(propertyNameString, out var vector2) == CompositionGetValueStatus.Succeeded)
			{
				_properties.InsertVector2(propertyNameString, UpdateVector2(subPropertyName, vector2, propertyValue));
			}
			else if (_properties.TryGetVector3(propertyNameString, out var vector3) == CompositionGetValueStatus.Succeeded)
			{
				_properties.InsertVector3(propertyNameString, UpdateVector3(subPropertyName, vector3, propertyValue));
			}
			else if (_properties.TryGetVector4(propertyNameString, out var vector4) == CompositionGetValueStatus.Succeeded)
			{
				_properties.InsertVector4(propertyNameString, UpdateVector4(subPropertyName, vector4, propertyValue));
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

	private object TryGetFromProperties(string propertyName, string subPropertyName)
	{
		if (_properties is not null)
		{
			if (_properties.TryGetBoolean(propertyName, out var @bool) == CompositionGetValueStatus.Succeeded && subPropertyName.Length == 0)
			{
				return @bool;
			}
			else if (_properties.TryGetColor(propertyName, out var color) == CompositionGetValueStatus.Succeeded)
			{
				return GetColor(subPropertyName, color);
			}
			else if (_properties.TryGetMatrix3x2(propertyName, out var matrix3x2) == CompositionGetValueStatus.Succeeded)
			{
				return GetMatrix3x2(subPropertyName, matrix3x2);
			}
			else if (_properties.TryGetMatrix4x4(propertyName, out var matrix4x4) == CompositionGetValueStatus.Succeeded)
			{
				return GetMatrix4x4(subPropertyName, matrix4x4);
			}
			else if (_properties.TryGetQuaternion(propertyName, out var quaternion) == CompositionGetValueStatus.Succeeded)
			{
				return GetQuaternion(subPropertyName, quaternion);
			}
			else if (_properties.TryGetScalar(propertyName, out var @float) == CompositionGetValueStatus.Succeeded && subPropertyName.Length == 0)
			{
				return @float;
			}
			else if (_properties.TryGetVector2(propertyName, out var vector2) == CompositionGetValueStatus.Succeeded)
			{
				return GetVector2(subPropertyName, vector2);
			}
			else if (_properties.TryGetVector3(propertyName, out var vector3) == CompositionGetValueStatus.Succeeded)
			{
				return GetVector3(subPropertyName, vector3);
			}
			else if (_properties.TryGetVector4(propertyName, out var vector4) == CompositionGetValueStatus.Succeeded)
			{
				return GetVector4(subPropertyName, vector4);
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
