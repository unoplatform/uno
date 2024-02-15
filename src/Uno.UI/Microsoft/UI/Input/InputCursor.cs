using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Core;

namespace Microsoft.UI.Input;

#if HAS_UNO_WINUI
public partial class InputCursor
#else
internal partial class InputCursor
#endif
{

	private List<WeakEventHelper.GenericEventHandler> _disposedHandlers = new List<WeakEventHelper.GenericEventHandler>();

	internal bool IsDisposed { get; private set; }

	protected InputCursor()
	{
	}

	public static InputCursor CreateFromCoreCursor(CoreCursor cursor) => InputSystemCursor.Create(cursor.Type.ToInputSystemCursorShape());

	public void Dispose()
	{
		if (!IsDisposed)
		{
			IsDisposed = true;
			var handlers = new List<WeakEventHelper.GenericEventHandler>(_disposedHandlers);
			foreach (var action in handlers)
			{
				action(this, null);
			}
		}
	}

	internal IDisposable RegisterDisposedEvent(EventHandler handler)
		=> WeakEventHelper.RegisterEvent(_disposedHandlers, handler, (h, s, a) => (h as EventHandler)?.Invoke(s, (EventArgs)a));

	// This assembly is not visible to CoreCursor's assembly, so we put the method here.
	internal static CoreCursor CreateCoreCursorFromInputSystemCursorShape(InputSystemCursorShape shape) => new CoreCursor(shape.ToCoreCursorType(), 0);
}
