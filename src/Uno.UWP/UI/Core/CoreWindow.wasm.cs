using Uno.Foundation;

using NativeMethods = __Windows.UI.Core.CoreWindow.NativeMethods;

namespace Windows.UI.Core;

public partial class CoreWindow
{
	private CoreCursor _pointerCursor = new CoreCursor(CoreCursorType.Arrow, 0);

	public global::Windows.UI.Core.CoreCursor PointerCursor
	{
		get => _pointerCursor;
		set
		{
			_pointerCursor = value;
			Internal_SetPointerCursor();
		}

	}

	private void Internal_SetPointerCursor()
	{
		NativeMethods.SetCursor(_pointerCursor.Type.ToCssCursor());
	}
}
