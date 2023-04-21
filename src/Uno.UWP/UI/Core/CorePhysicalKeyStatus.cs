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

		public override int GetHashCode() => HashCode.Combine(RepeatCount, ScanCode, IsExtendedKey, IsMenuKeyDown,
			WasKeyDown, IsKeyReleased);

		public static bool operator ==(CorePhysicalKeyStatus left, CorePhysicalKeyStatus right) => left.Equals(right);

		public static bool operator !=(CorePhysicalKeyStatus left, CorePhysicalKeyStatus right) => !(left == right);
	}
}
