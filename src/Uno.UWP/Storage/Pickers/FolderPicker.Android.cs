#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Uno.UI;

namespace Windows.Storage.Pickers
{
	public partial class FolderPicker
	{
		internal const int RequestCode = 6001;
		private Action<Intent>? _intentAction;

		private static TaskCompletionSource<Intent?>? _currentFolderPickerRequest;

		internal static bool TryHandleIntent(Intent intent, Result resultCode)
		{
			if (_currentFolderPickerRequest == null)
			{
				return false;
			}
			if (resultCode == Result.Canceled)
			{
				_currentFolderPickerRequest.SetResult(null);
			}
			else
			{
				_currentFolderPickerRequest.SetResult(intent);
			}
			return true;
		}

		public async Task<StorageFolder?> PickSingleFolderTaskAsync(CancellationToken token)
		{
			if (!(ContextHelper.Current is Activity appActivity))
			{
				throw new InvalidOperationException("Application activity is not yet set, API called too early.");
			}
			var intent = new Intent(Intent.ActionOpenDocumentTree);
			_currentFolderPickerRequest = new TaskCompletionSource<Intent?>();

			_intentAction?.Invoke(intent);

			appActivity.StartActivityForResult(intent, RequestCode);

			var resultIntent = await _currentFolderPickerRequest.Task;
			_currentFolderPickerRequest = null;

			if (resultIntent?.Data != null)
			{
				return StorageFolder.GetFromSafUri(resultIntent.Data);
			}
			else if (resultIntent?.ClipData != null && resultIntent.ClipData.ItemCount > 0)
			{
				for (int itemIndex = 0; itemIndex < resultIntent.ClipData.ItemCount; itemIndex++)
				{
					var uri = resultIntent.ClipData.GetItemAt(itemIndex)?.Uri;
					if (uri != null)
					{
						return StorageFolder.GetFromSafUri(uri);
					}
				}
			}

			return null;
		}

		internal void RegisterOnBeforeStartActivity(Action<Intent> intentAction)
			=> _intentAction = intentAction;
	}
}
