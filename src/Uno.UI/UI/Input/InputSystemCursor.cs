using System;
namespace Microsoft.UI.Input;

public sealed partial class InputSystemCursor : InputCursor
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
