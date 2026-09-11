#nullable enable

namespace Windows.UI.Core;

public partial class CharacterReceivedEventArgs : ICoreWindowEventArgs
{
	internal CharacterReceivedEventArgs(uint keyCode, CorePhysicalKeyStatus keyStatus)
	{
		KeyCode = keyCode;
		KeyStatus = keyStatus;
	}

	public bool Handled { get; set; }

	public uint KeyCode { get; }

	public CorePhysicalKeyStatus KeyStatus { get; }
}
