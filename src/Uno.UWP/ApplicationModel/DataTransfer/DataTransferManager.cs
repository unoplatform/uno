#nullable enable

#if __WASM__ || __IOS__ || __ANDROID__ || __MACOS__ || __SKIA__
using System;
using Windows.Foundation;
using Uno.Logging;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataTransferManager
	{
		private static Lazy<DataTransferManager> _instance = new Lazy<DataTransferManager>(() => new DataTransferManager());

		private DataTransferManager()
		{
		}

		public event TypedEventHandler<DataTransferManager, DataRequestedEventArgs>? DataRequested;

		public static DataTransferManager GetForCurrentView() => _instance.Value;

		public static void ShowShareUI() => ShowShareUI(new ShareUIOptions());

		public static void ShowShareUI(ShareUIOptions options)
		{
			var dataTransferManager = _instance.Value;
			var args = new DataRequestedEventArgs((dataRequest)=>ContinueShowShareUI(options, dataRequest.Data));
			dataTransferManager.DataRequested?.Invoke(dataTransferManager, args);

			// Data retrieval may be done asynchronously, check for deferral use
			if (!args.Request.IsDeferred)
			{
				ContinueShowShareUI(options, args.Request.Data);
			}
			else
			{
				args.Request.EventRaiseCompleted();
			}
		}

		private static async void ContinueShowShareUI(ShareUIOptions options, DataPackage dataPackage)
		{
			try
			{
				// Because showing the Share UI is a fire-and-forget operation
				// and retrieving data from DataPackage requires async-await,
				// this method must be async void.
				var result = await ShowShareUIAsync(options, dataPackage);
				if (result)
				{
					dataPackage.OnShareCompleted();
				}
				else
				{
					dataPackage.OnShareCanceled();
				}
			}
			catch (Exception ex)
			{
				if (_instance.Value.Log().IsEnabled(LogLevel.Error))
				{
					_instance.Value.Log().LogError($"Exception occurred trying to show share UI: {ex}");
				}
				dataPackage.OnShareCanceled();
			}
		}

		internal static async Task<Uri?> GetSharedUriAsync(DataPackageView view)
		{
			if (view.Contains(StandardDataFormats.Uri))
			{
				return await view.GetUriAsync();
			}
			else if (view.Contains(StandardDataFormats.WebLink))
			{
				return await view.GetWebLinkAsync();
			}
			else if (view.Contains(StandardDataFormats.ApplicationLink))
			{
				return await view.GetApplicationLinkAsync();
			}

			return null;
		}
	}
}
#endif
