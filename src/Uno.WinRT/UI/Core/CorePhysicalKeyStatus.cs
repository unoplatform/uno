#nullable enable

using System;

namespace Windows.UI.Core
{
	public partial struct CorePhysicalKeyStatus
	{
		public uint RepeatCount;
		public uint ScanCode;
		public bool IsExtendedKey;
		public bool IsMenuKeyDown;
		public bool WasKeyDown;
		public bool IsKeyReleased;

		public bool Equals(CorePhysicalKeyStatus other) =>
			RepeatCount == other.RepeatCount && ScanCode == other.ScanCode &&
			IsExtendedKey == other.IsExtendedKey && IsMenuKeyDown == other.IsMenuKeyDown &&
			WasKeyDown == other.WasKeyDown && IsKeyReleased == other.IsKeyReleased;

		public override bool Equals(object? obj) => obj is CorePhysicalKeyStatus other && Equals(other);

		public override int GetHashCode()
		{
			var hashCode = -1614557298;
			hashCode = hashCode * -1521134295 + RepeatCount.GetHashCode();
			hashCode = hashCode * -1521134295 + ScanCode.GetHashCode();
			hashCode = hashCode * -1521134295 + IsExtendedKey.GetHashCode();
			hashCode = hashCode * -1521134295 + IsMenuKeyDown.GetHashCode();
			hashCode = hashCode * -1521134295 + WasKeyDown.GetHashCode();
			hashCode = hashCode * -1521134295 + IsKeyReleased.GetHashCode();
			return hashCode;
		}

		public static bool operator ==(CorePhysicalKeyStatus left, CorePhysicalKeyStatus right) => left.Equals(right);

		public static bool operator !=(CorePhysicalKeyStatus left, CorePhysicalKeyStatus right) => !(left == right);
	}
}
