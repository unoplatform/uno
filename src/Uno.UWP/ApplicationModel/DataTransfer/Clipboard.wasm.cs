using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uno.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private const string JsType = "Uno.Utils.Clipboard";

		public static void Clear() => SetClipboardText(string.Empty);

		public static void SetContent(DataPackage content)
		{
			if (content?.Text != null)
			{
				SetClipboardText(content.Text);
			}
		}

		public static DataPackageView GetContent()
		{
			var dataPackageView = new DataPackageView();

			var command = $"{JsType}.getText()";
			var getTextTask = WebAssemblyRuntime.InvokeAsync(command);
			dataPackageView.SetFormatTask(StandardDataFormats.Text, getTextTask);
			
			return dataPackageView;
		}

		private static void SetClipboardText(string text)
		{
			var escapedText = WebAssemblyRuntime.EscapeJs(text);
			var command = $"{JsType}.setText(\"{escapedText}\");";
			WebAssemblyRuntime.InvokeJS(command);
		}

		private static void StartContentChanged()
		{
			var command = $"{JsType}.startContentChanged()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		private static void StopContentChanged()
		{
			var command = $"{JsType}.stopContentChanged()";
			WebAssemblyRuntime.InvokeJS(command);
		}

		public static int DispatchContentChanged()
		{
			OnContentChanged();
			return 0;
		}
	}
}
