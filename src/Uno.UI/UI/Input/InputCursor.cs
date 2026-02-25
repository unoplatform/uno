using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.UI.Core;

namespace Microsoft.UI.Input;

public partial class InputCursor
{
	private WeakEventHelper.WeakEventCollection _disposedHandlers = new();

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

			_disposedHandlers.Invoke(this, null);
			_disposedHandlers.Dispose();
		}
	}

	internal IDisposable RegisterDisposedEvent(EventHandler handler)
		=> WeakEventHelper.RegisterEvent(_disposedHandlers, handler, (h, s, a) => (h as EventHandler)?.Invoke(s, (EventArgs)a));

	// This assembly is not visible to CoreCursor's assembly, so we put the method here.
	internal static CoreCursor CreateCoreCursorFromInputSystemCursorShape(InputSystemCursorShape shape) => new CoreCursor(shape.ToCoreCursorType(), 0);
}
