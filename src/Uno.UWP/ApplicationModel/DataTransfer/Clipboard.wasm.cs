using Uno.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		public static void SetContent(DataPackage content)
		{
			if (content?.Text != null)
			{
				var text = WebAssemblyRuntime.EscapeJs(content.Text);
				var command = $"Uno.Utils.Clipboard.setText(\"{text}\");";

				WebAssemblyRuntime.InvokeJS(command);
			}
		}
	}
}
