#nullable enable

namespace Uno.UI.Runtime.MacOS;

internal static class MacOSKeyboardInput
{
	public static char? FilterTextInputCharacter(ushort unicode)
	{
		if (unicode == 0)
		{
			return null;
		}

		var character = (char)unicode;
		// Navigation keys must not also insert text. CR/LF remain available for multiline input.
		if (char.IsControl(character) && character is not '\r' and not '\n')
		{
			return null;
		}

		return character;
	}
}
