#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;
using Color = Windows.UI.Color;

namespace Microsoft.UI.Composition
{
	public partial class CompositionPropertySet : CompositionObject
	{
		private readonly Dictionary<string, object> _properties = new Dictionary<string, object>();

		internal CompositionPropertySet(Compositor compositor) : base(compositor)
		{
		}

		public void InsertColor(string propertyName, Color value) => SetValue(propertyName, value);

		public void InsertMatrix3x2(string propertyName, Matrix3x2 value) => SetValue(propertyName, value);

		public void InsertMatrix4x4(string propertyName, Matrix4x4 value) => SetValue(propertyName, value);

		public void InsertQuaternion(string propertyName, Quaternion value) => SetValue(propertyName, value);

		public void InsertScalar(string propertyName, float value) => SetValue(propertyName, value);

		public void InsertVector2(string propertyName, Vector2 value) => SetValue(propertyName, value);

		public void InsertVector3(string propertyName, Vector3 value) => SetValue(propertyName, value);

		public void InsertVector4(string propertyName, Vector4 value) => SetValue(propertyName, value);

		public void InsertBoolean(string propertyName, bool value) => SetValue(propertyName, value);

		public CompositionGetValueStatus TryGetColor(string propertyName, out Color value) => TryGetValue(propertyName, out value);

		public CompositionGetValueStatus TryGetMatrix3x2(string propertyName, out Matrix3x2 value) => TryGetValue(propertyName, out value);

		public CompositionGetValueStatus TryGetMatrix4x4(string propertyName, out Matrix4x4 value) => TryGetValue(propertyName, out value);

		public CompositionGetValueStatus TryGetQuaternion(string propertyName, out Quaternion value) => TryGetValue(propertyName, out value);

		public CompositionGetValueStatus TryGetScalar(string propertyName, out float value) => TryGetValue(propertyName, out value);

		public CompositionGetValueStatus TryGetVector2(string propertyName, out Vector2 value) => TryGetValue(propertyName, out value);

		public CompositionGetValueStatus TryGetVector3(string propertyName, out Vector3 value) => TryGetValue(propertyName, out value);

		public CompositionGetValueStatus TryGetVector4(string propertyName, out Vector4 value) => TryGetValue(propertyName, out value);

		public CompositionGetValueStatus TryGetBoolean(string propertyName, out bool value) => TryGetValue(propertyName, out value);

		private CompositionGetValueStatus TryGetValue<T>(string propertyName, out T value)
			where T : struct
		{
			value = default;
			if (_properties.TryGetValue(propertyName, out var objValue))
			{
				if (objValue is T matchingValue)
				{
					value = matchingValue;
					return CompositionGetValueStatus.Succeeded;
				}
				return CompositionGetValueStatus.TypeMismatch;
			}
			return CompositionGetValueStatus.NotFound;
		}

		private void SetValue<T>(string propertyName, T value)
			where T : struct
		{
			if (_properties.TryGetValue(propertyName, out var existingValue) && !(existingValue is T _))
			{
				throw new ArgumentException("Canot insert a different type for an existing property.");
			}

			_properties[propertyName] = value;
		}

		private protected override bool IsAnimatableProperty(string propertyName)
			=> _properties.ContainsKey(propertyName);
	}
}
