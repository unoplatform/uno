#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
		private const string JsType = "Windows.ApplicationModel.DataTransfer.DataTransferManager";
		
		public static bool IsSupported() => bool.TryParse(WebAssemblyRuntime.InvokeJS($"{JsType}.isSupported()"), out var result) && result;

		private static async Task<bool> ShowShareUIAsync(ShareUIOptions options, DataPackage dataPackage)
		{
			var dataPackageView = dataPackage.GetView();

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

			var uri = await GetSharedUriAsync(dataPackageView);

			var uriText = uri != null ? $"\"{WebAssemblyRuntime.EscapeJs(uri.OriginalString)}\"" : null;

			var result = await WebAssemblyRuntime.InvokeAsync($"{JsType}.showShareUI({title ?? "null"},{text ?? "null"},{uriText ?? "null"})");
			return bool.TryParse(result, out var boolResult) && boolResult;
		}
	}
}
