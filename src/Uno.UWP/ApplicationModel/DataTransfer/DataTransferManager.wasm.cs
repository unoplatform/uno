#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Foundation;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.ApplicationModel.DataTransfer.DataTransferManager.NativeMethods;
#endif

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.ApplicationModel.DataTransfer.DataTransferManager";
#endif

		public static bool IsSupported() =>
#if NET7_0_OR_GREATER
			NativeMethods.IsSupported();
#else
			bool.TryParse(WebAssemblyRuntime.InvokeJS($"{JsType}.isSupported()"), out var result) && result;
#endif

		private static async Task<bool> ShowShareUIAsync(ShareUIOptions options, DataPackage dataPackage)
		{
			var dataPackageView = dataPackage.GetView();

			string? text;
			if (dataPackageView.Contains(StandardDataFormats.Text))
			{
				text = await dataPackageView.GetTextAsync();
			}
			else
			{
				text = dataPackage.Properties.Description;
			}

			var uri = await GetSharedUriAsync(dataPackageView);

#if NET7_0_OR_GREATER
			var result = await NativeMethods.ShowShareUIAsync(dataPackage.Properties.Title, text, uri?.OriginalString);
#else
			var title = dataPackage.Properties.Title != null ? $"\"{WebAssemblyRuntime.EscapeJs(dataPackage.Properties.Title)}\"" : null;
			text = text != null ? $"\"{WebAssemblyRuntime.EscapeJs(text)}\"" : null;
			var uriText = uri != null ? $"\"{WebAssemblyRuntime.EscapeJs(uri.OriginalString)}\"" : null;

			var result = await WebAssemblyRuntime.InvokeAsync($"{JsType}.showShareUI({title ?? "null"},{text ?? "null"},{uriText ?? "null"})");
#endif

			return bool.TryParse(result, out var boolResult) && boolResult;
		}
	}
}
