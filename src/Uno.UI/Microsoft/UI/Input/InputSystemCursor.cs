#nullable disable

namespace Microsoft.UI.Input;

#if HAS_UNO_WINUI
public sealed partial class InputSystemCursor : InputCursor
#else
internal sealed partial class InputSystemCursor : InputCursor
#endif
{
	private InputSystemCursor(InputSystemCursorShape type)
	{
		CursorShape = type;
	}

	public InputSystemCursorShape CursorShape { get; }

	public static InputSystemCursor Create(InputSystemCursorShape type)
		=> new InputSystemCursor(type);
}
