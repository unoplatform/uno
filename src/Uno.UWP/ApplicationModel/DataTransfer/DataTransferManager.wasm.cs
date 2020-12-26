#nullable enable

using System;
using Uno.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
		private const string JsType = "Windows.ApplicationModel.DataTransfer.DataTransferManager";

		private static Lazy<DataTransferManager> _instance = new Lazy<DataTransferManager>(() => new DataTransferManager());

		public static bool IsSupported() => bool.TryParse(WebAssemblyRuntime.InvokeJS($"{JsType}.isSupported()"), out var result) && result;

		public static DataTransferManager GetForCurrentView() => _instance.Value;

		public static async void ShowShareUI()
		{
			var dataTransferManager = _instance.Value;
			var args = new DataRequestedEventArgs();
			dataTransferManager.DataRequested?.Invoke(dataTransferManager, args);
			var dataPackage = args.Request.Data;
			var dataPackageView = args.Request.Data.GetView();

			var title = dataPackage.Properties.Title != null ? $"\"{WebAssemblyRuntime.EscapeJs(dataPackage.Properties.Title)}\"" : null;

			string? text;
			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				text = await dataPackageView.GetTextAsync();
			}
			else
			{
				text = dataPackage.Properties.Description;
			}
			text = text != null ? $"\"{WebAssemblyRuntime.EscapeJs(text)}\"" : null;

			Uri? uri = null;
			if (dataPackageView.Contains(StandardDataFormats.Uri))
			{
				uri = await dataPackageView.GetUriAsync();
			}
			else if (dataPackageView.Contains(StandardDataFormats.WebLink))
			{
				uri = await dataPackageView.GetWebLinkAsync();
			}
			else if (dataPackageView.Contains(StandardDataFormats.ApplicationLink))
			{
				uri = await dataPackageView.GetApplicationLinkAsync();
			}

			var uriText = uri != null ? $"\"{WebAssemblyRuntime.EscapeJs(uri.ToString())}\"" : null;

			var result = await WebAssemblyRuntime.InvokeAsync($"{JsType}.showShareUI({title ?? "null"},{text ?? "null"},{uriText ?? "null"})");
			if (bool.TryParse(result, out var boolResult) && boolResult)
			{
				dataPackage.OnShareCompleted();
			}
			else
			{
				dataPackage.OnShareCanceled();
			}
		}
	}
}
