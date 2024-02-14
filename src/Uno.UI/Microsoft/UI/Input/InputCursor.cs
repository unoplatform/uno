using System.ComponentModel;
using Windows.UI.Core;

namespace Microsoft.UI.Input;

#if HAS_UNO_WINUI
public partial class InputCursor
#else
internal partial class InputCursor
#endif
{
	internal bool IsDisposed { get; private set; }

	protected InputCursor()
	{
	}

	public static InputCursor CreateFromCoreCursor(CoreCursor cursor) => InputSystemCursor.Create(cursor.Type.ToInputSystemCursorShape());

	public void Dispose() => IsDisposed = true;

	// This assembly is not visible to CoreCursor's assembly, so we put the method here.
	internal static CoreCursor CreateCoreCursorFromInputSystemCursorShape(InputSystemCursorShape shape) => new CoreCursor(shape.ToCoreCursorType(), 0);
}
