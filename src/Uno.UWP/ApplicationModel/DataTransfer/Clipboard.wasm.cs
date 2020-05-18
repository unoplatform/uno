using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Uno.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		private const string JsType = "Uno.Utils.Clipboard";

		private static Dictionary<string, TaskCompletionSource<string>> _getTextRequests =
			new Dictionary<string, TaskCompletionSource<string>>();

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

			// get clipboard text
			var getTextRequestId = Guid.NewGuid().ToString();
			var getTextCompletionSource = new TaskCompletionSource<string>();
			dataPackageView.SetTextTask(getTextCompletionSource.Task);
			_getTextRequests.Add(getTextRequestId, getTextCompletionSource);
			var command = $"{JsType}.getText(\"{getTextRequestId}\");";
			WebAssemblyRuntime.InvokeJS(command);

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

		public static int DispatchGetContent(string requestId, string content)
		{
			// If user didn't grant permission, content will be null.
			// in such case, we just return empty string.
			_getTextRequests[requestId].SetResult(content ?? "");
			return 0;
		}
	}
}
