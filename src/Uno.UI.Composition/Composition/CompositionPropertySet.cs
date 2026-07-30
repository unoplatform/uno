#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Windows.Storage.Streams;
using Color = Windows.UI.Color;

namespace Microsoft.UI.Composition
{
	public partial class CompositionPropertySet : CompositionObject
	{
		private readonly Dictionary<string, object> _properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		internal CompositionPropertySet(Compositor compositor) : base(compositor)
		{
		}

		// The CompositionObject this property set was created for via the Properties getter.
		// Property changes on this set need to bubble up to the owner so expression animations
		// referencing the owner (e.g. `_.Progress`) re-evaluate when the property set changes.
		internal CompositionObject? Owner { get; set; }

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

		internal CompositionGetValueStatus TryGetValue<T>(string propertyName, out T value)
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

		internal bool TryGetValueNonGeneric(string propertyName, [NotNullWhen(true)] out object? value)
		{
			return _properties.TryGetValue(propertyName, out value);
		}

		private void SetValue<T>(string propertyName, T value)
			where T : struct
		{
			if (_properties.TryGetValue(propertyName, out var existingValue))
			{
				if (existingValue is not T _)
				{
					throw new ArgumentException("Cannot insert a different type for an existing property.");
				}

				if (EqualityComparer<T>.Default.Equals((T)existingValue, value))
				{
					return;
				}
			}

			_properties[propertyName] = value;

			// Notify any expression animations and bindings that hold a reference to this property set
			// so they re-evaluate against the new value. WinUI's CompositionPropertySet propagates
			// changes via DComp dependency-tracking; we replicate it here through the Uno context system.
			OnPropertyChanged(propertyName, false);

			// Also notify the owning CompositionObject so expression animations that referenced
			// that object (e.g. `_.Progress` where _ resolves to a Visual whose Properties contain
			// `Progress`) get re-evaluated. Property access via `obj.Foo` is resolved through the
			// owner's GetAnimatableProperty, so the expression's listener was registered on the
			// owner rather than on this property set.
			if (Owner is { } owner && owner != this)
			{
				owner.PropagateChanged();
			}
		}
	}
}
