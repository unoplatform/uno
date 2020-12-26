#nullable enable

using System;
using System.Threading.Tasks;
using Uno.ApplicationModel.DataTransfer;
using Uno.Foundation.Extensibility;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
		private static readonly Lazy<IDataTransferManagerExtension?> _dataTransferManagerExtension = new Lazy<IDataTransferManagerExtension?>(() =>
		{
			if (ApiExtensibility.CreateInstance(typeof(DataTransferManager), out IDataTransferManagerExtension dataTransferManagerExtension))
			{
				return dataTransferManagerExtension;
			}
			return null;
		});

		public static bool IsSupported() => _dataTransferManagerExtension.Value?.IsSupported() == true;

		private static async Task<bool> ShowShareUIAsync(ShareUIOptions options, DataPackage dataPackage)
		{
			var extension = _dataTransferManagerExtension.Value;
			if(extension != null)
			{
				return await extension.ShowShareUIAsync(options, dataPackage);
			}
			return false;
		}
	}
}
