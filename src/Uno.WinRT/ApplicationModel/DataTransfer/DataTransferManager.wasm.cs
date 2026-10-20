#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Foundation;

using NativeMethods = __Windows.ApplicationModel.DataTransfer.DataTransferManager.NativeMethods;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
		public static bool IsSupported() => NativeMethods.IsSupported();

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
			var result = await NativeMethods.ShowShareUIAsync(dataPackage.Properties.Title, text, uri?.OriginalString);
			return bool.TryParse(result, out var boolResult) && boolResult;
		}
	}
}
