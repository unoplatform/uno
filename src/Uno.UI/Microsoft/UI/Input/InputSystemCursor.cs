#if HAS_UNO_WINUI

namespace Microsoft.UI.Input
{
	public sealed class InputSystemCursor : InputCursor
	{
		private InputSystemCursor(InputSystemCursorShape type)
		{
			CursorShape = type;
		}

		public InputSystemCursorShape CursorShape { get; }

		public static InputSystemCursor Create(InputSystemCursorShape type)
			=> new InputSystemCursor(type);
	}
}
#endif
