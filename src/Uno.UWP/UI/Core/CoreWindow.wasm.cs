using Uno.Foundation;

namespace Windows.UI.Core
{
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
			var command = string.Concat(new[]
			{
				"Uno.UI.WindowManager.current.setCursor(\"",
				_pointerCursor.Type.ToCssCursor(),
				"\");"
			});

			WebAssemblyRuntime.InvokeJS(command);
		}
	}
}
