#nullable enable

using System;
using System.Globalization;
using System.Numerics;
using Windows.UI;

namespace Windows.UI.Composition;

internal static class SubPropertyHelpers
{
	internal static T ValidateValue<T>(object? value)
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

	internal static Color UpdateColor(ReadOnlySpan<char> subPropertyName, Color existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Color>(propertyValue);
		}

		if (subPropertyName.Equals("A", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.A = ValidateValue<byte>(propertyValue);
		}
		else if (subPropertyName.Equals("R", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.R = ValidateValue<byte>(propertyValue);
		}
		else if (subPropertyName.Equals("G", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.G = ValidateValue<byte>(propertyValue);
		}
		else if (subPropertyName.Equals("B", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.B = ValidateValue<byte>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	internal static Matrix3x2 UpdateMatrix3x2(ReadOnlySpan<char> subPropertyName, Matrix3x2 existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Matrix3x2>(propertyValue);
		}

		if (subPropertyName.Equals("M11", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M11 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M12", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M12 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M21", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M21 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M22", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M22 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M31", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M31 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M32", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M32 = ValidateValue<float>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update Matrix3x2 with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	internal static Matrix4x4 UpdateMatrix4x4(ReadOnlySpan<char> subPropertyName, Matrix4x4 existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Matrix4x4>(propertyValue);
		}

		if (subPropertyName.Equals("M11", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M11 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M12", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M12 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M13", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M13 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M14", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M14 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M21", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M21 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M22", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M22 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M23", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M23 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M24", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M24 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M31", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M31 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M32", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M32 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M33", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M33 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M34", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M34 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M41", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M41 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M42", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M42 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M43", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M43 = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("M44", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.M44 = ValidateValue<float>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update Matrix3x2 with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	internal static Quaternion UpdateQuaternion(ReadOnlySpan<char> subPropertyName, Quaternion existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Quaternion>(propertyValue);
		}

		if (subPropertyName.Equals("X", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.X = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.Y = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Z", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.Z = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("W", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.W = ValidateValue<float>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	internal static Vector2 UpdateVector2(ReadOnlySpan<char> subPropertyName, Vector2 existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Vector2>(propertyValue);
		}

		if (subPropertyName.Equals("X", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.X = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.Y = ValidateValue<float>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	internal static Vector3 UpdateVector3(ReadOnlySpan<char> subPropertyName, Vector3 existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Vector3>(propertyValue);
		}

		if (subPropertyName.Equals("X", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.X = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.Y = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Z", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.Z = ValidateValue<float>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	internal static Vector4 UpdateVector4(ReadOnlySpan<char> subPropertyName, Vector4 existingValue, object? propertyValue)
	{
		if (subPropertyName.Length == 0)
		{
			return ValidateValue<Vector4>(propertyValue);
		}

		if (subPropertyName.Equals("X", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.X = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.Y = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("Z", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.Z = ValidateValue<float>(propertyValue);
		}
		else if (subPropertyName.Equals("W", StringComparison.OrdinalIgnoreCase))
		{
			existingValue.W = ValidateValue<float>(propertyValue);
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}

		return existingValue;
	}

	internal static object GetColor(string subPropertyName, Color existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("A", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.A;
		}
		else if (subPropertyName.Equals("R", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.R;
		}
		else if (subPropertyName.Equals("G", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.G;
		}
		else if (subPropertyName.Equals("B", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.B;
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}
	}

	internal static object GetMatrix3x2(string subPropertyName, Matrix3x2 existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("M11", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M11;
		}
		else if (subPropertyName.Equals("M12", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M12;
		}
		else if (subPropertyName.Equals("M21", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M21;
		}
		else if (subPropertyName.Equals("M22", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M22;
		}
		else if (subPropertyName.Equals("M31", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M31;
		}
		else if (subPropertyName.Equals("M32", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M32;
		}
		else
		{
			throw new Exception($"Cannot update Matrix3x2 with a sub-property named '{subPropertyName}'.");
		}
	}

	internal static object GetMatrix4x4(string subPropertyName, Matrix4x4 existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("M11", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M11;
		}
		else if (subPropertyName.Equals("M12", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M12;
		}
		else if (subPropertyName.Equals("M13", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M13;
		}
		else if (subPropertyName.Equals("M14", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M14;
		}
		else if (subPropertyName.Equals("M21", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M21;
		}
		else if (subPropertyName.Equals("M22", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M22;
		}
		else if (subPropertyName.Equals("M23", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M23;
		}
		else if (subPropertyName.Equals("M24", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M24;
		}
		else if (subPropertyName.Equals("M31", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M31;
		}
		else if (subPropertyName.Equals("M32", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M32;
		}
		else if (subPropertyName.Equals("M33", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M33;
		}
		else if (subPropertyName.Equals("M34", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M34;
		}
		else if (subPropertyName.Equals("M41", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M41;
		}
		else if (subPropertyName.Equals("M42", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M42;
		}
		else if (subPropertyName.Equals("M43", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M43;
		}
		else if (subPropertyName.Equals("M44", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.M44;
		}
		else
		{
			throw new Exception($"Cannot update Matrix3x2 with a sub-property named '{subPropertyName}'.");
		}
	}

	internal static object GetQuaternion(string subPropertyName, Quaternion existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("X", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.X;
		}
		else if (subPropertyName.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.Y;
		}
		else if (subPropertyName.Equals("Z", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.Z;
		}
		else if (subPropertyName.Equals("W", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.W;
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}
	}

	internal static object GetVector2(string subPropertyName, Vector2 existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("X", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.X;
		}
		else if (subPropertyName.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.Y;
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}
	}

	internal static object GetVector3(string subPropertyName, Vector3 existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("X", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.X;
		}
		else if (subPropertyName.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.Y;
		}
		else if (subPropertyName.Equals("Z", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.Z;
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}
	}

	internal static object GetVector4(string subPropertyName, Vector4 existingValue)
	{
		if (subPropertyName.Length == 0)
		{
			return existingValue;
		}

		if (subPropertyName.Equals("X", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.X;
		}
		else if (subPropertyName.Equals("Y", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.Y;
		}
		else if (subPropertyName.Equals("Z", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.Z;
		}
		else if (subPropertyName.Equals("W", StringComparison.OrdinalIgnoreCase))
		{
			return existingValue.W;
		}
		else
		{
			throw new Exception($"Cannot update color with a sub-property named '{subPropertyName}'.");
		}
	}

	internal static void TryUpdateFromProperties(CompositionPropertySet? properties, ReadOnlySpan<char> propertyName, ReadOnlySpan<char> subPropertyName, object? propertyValue)
	{
		if (properties is not null)
		{
			var propertyNameString = propertyName.ToString();
			if (properties.TryGetBoolean(propertyNameString, out _) == CompositionGetValueStatus.Succeeded)
			{
				properties.InsertBoolean(propertyNameString, ValidateValue<bool>(propertyValue));
			}
			else if (properties.TryGetColor(propertyNameString, out var color) == CompositionGetValueStatus.Succeeded)
			{
				properties.InsertColor(propertyNameString, UpdateColor(subPropertyName, color, propertyValue));
			}
			else if (properties.TryGetMatrix3x2(propertyNameString, out var matrix3x2) == CompositionGetValueStatus.Succeeded)
			{
				properties.InsertMatrix3x2(propertyNameString, UpdateMatrix3x2(subPropertyName, matrix3x2, propertyValue));
			}
			else if (properties.TryGetMatrix4x4(propertyNameString, out var matrix4x4) == CompositionGetValueStatus.Succeeded)
			{
				properties.InsertMatrix4x4(propertyNameString, UpdateMatrix4x4(subPropertyName, matrix4x4, propertyValue));
			}
			else if (properties.TryGetQuaternion(propertyNameString, out var quaternion) == CompositionGetValueStatus.Succeeded)
			{
				properties.InsertQuaternion(propertyNameString, UpdateQuaternion(subPropertyName, quaternion, propertyValue));
			}
			else if (properties.TryGetScalar(propertyNameString, out _) == CompositionGetValueStatus.Succeeded)
			{
				properties.InsertScalar(propertyNameString, ValidateValue<float>(propertyValue));
			}
			else if (properties.TryGetVector2(propertyNameString, out var vector2) == CompositionGetValueStatus.Succeeded)
			{
				properties.InsertVector2(propertyNameString, UpdateVector2(subPropertyName, vector2, propertyValue));
			}
			else if (properties.TryGetVector3(propertyNameString, out var vector3) == CompositionGetValueStatus.Succeeded)
			{
				properties.InsertVector3(propertyNameString, UpdateVector3(subPropertyName, vector3, propertyValue));
			}
			else if (properties.TryGetVector4(propertyNameString, out var vector4) == CompositionGetValueStatus.Succeeded)
			{
				properties.InsertVector4(propertyNameString, UpdateVector4(subPropertyName, vector4, propertyValue));
			}
			else
			{
				throw new Exception($"Unable to set property '{propertyName}'");
			}
		}
		else
		{
			throw new Exception($"Unable to set property '{propertyName}'");
		}
	}

	internal static object TryGetFromProperties(CompositionPropertySet? properties, string propertyName, string subPropertyName)
	{
		if (properties?.TryGetValueNonGeneric(propertyName, out var value) == true)
		{
			return subPropertyName.Length == 0 ? value : GetSubProperty(subPropertyName, value);
		}

		throw new Exception($"Unable to get property '{propertyName}'.");
	}

	internal static object GetSubProperty(string subPropertyName, object value)
	{
		if (value is Vector2 vector2)
		{
			return GetVector2(subPropertyName, vector2);
		}
		else if (value is Vector3 vector3)
		{
			return GetVector3(subPropertyName, vector3);
		}
		else if (value is Vector4 vector4)
		{
			return GetVector4(subPropertyName, vector4);
		}
		else if (value is Color color)
		{
			return GetColor(subPropertyName, color);
		}
		else if (value is Quaternion quaternion)
		{
			return GetQuaternion(subPropertyName, quaternion);
		}
		else if (value is Matrix3x2 matrix3x2)
		{
			return GetMatrix3x2(subPropertyName, matrix3x2);
		}
		else if (value is Matrix4x4 matrix4x4)
		{
			return GetMatrix4x4(subPropertyName, matrix4x4);
		}

		throw new ArgumentException($"Cannot find property '{subPropertyName}' on object of type '{value?.GetType()}'.");
	}
}
