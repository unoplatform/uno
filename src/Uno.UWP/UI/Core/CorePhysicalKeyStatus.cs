#nullable enable

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
	}
}
