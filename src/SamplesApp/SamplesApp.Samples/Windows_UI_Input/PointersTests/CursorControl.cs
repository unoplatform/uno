using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Input.PointersTests
{
	/// <summary>
	/// A simple <see cref="ContentControl"/> that exposes the protected cursor via a settable
	/// <see cref="CursorShape"/> property, so XAML samples can demonstrate per-element cursors
	/// (driven through UIElement.ProtectedCursor -> CoreWindow.PointerCursor).
	/// </summary>
	public partial class CursorControl : ContentControl
	{
		private InputSystemCursorShape _cursorShape = InputSystemCursorShape.Arrow;

		public InputSystemCursorShape CursorShape
		{
			get => _cursorShape;
			set
			{
				_cursorShape = value;
				ProtectedCursor = InputSystemCursor.Create(value);
			}
		}
	}
}
