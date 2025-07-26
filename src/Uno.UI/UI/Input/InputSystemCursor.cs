using System;
namespace Microsoft.UI.Input;

#if HAS_UNO_WINUI
public sealed partial class InputSystemCursor : InputCursor
#else
internal sealed partial class InputSystemCursor : InputCursor
#endif
{
	private readonly InputSystemCursorShape _cursorShape;

	private InputSystemCursor(InputSystemCursorShape type)
	{
		_cursorShape = type;
	}

	public InputSystemCursorShape CursorShape
	{
		get
		{
			if (IsDisposed)
			{
				throw new ObjectDisposedException(nameof(CursorShape));
			}

			return _cursorShape;
		}
	}

	public static InputSystemCursor Create(InputSystemCursorShape type)
		=> new InputSystemCursor(type);
}
