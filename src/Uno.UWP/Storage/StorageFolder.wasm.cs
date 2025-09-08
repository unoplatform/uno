using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Uno.Extensions;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using NativeMethods = __Windows.Storage.StorageFolder.NativeMethods;

namespace Windows.Storage
{
	partial class StorageFolder
	{
		private static TaskCompletionSource<bool> _storageInitialized = new TaskCompletionSource<bool>();

		internal async Task MakePersistentAsync()
			=> await MakePersistentAsync(this);

		private static async Task TryInitializeStorage()
		{
			if (typeof(StorageFolder).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(StorageFolder).Log().Debug("Waiting for emscripten storage initialization");
			}

			await _storageInitialized.Task;

			if (typeof(StorageFolder).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(StorageFolder).Log().Debug("Emscripten storage initialized");
			}
		}

		internal static async Task MakePersistentAsync(params StorageFolder[] folders)
			=> await NativeMethods.MakePersistentAsync(folders.SelectToArray(f => f.Path));

		[JSExport]
		internal static void DispatchStorageInitialized()
		{
			if (typeof(StorageFolder).Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				typeof(StorageFolder).Log().Debug("Dispatch emscripten storage initialized");
			}

			_storageInitialized.TrySetResult(true);
		}

	}
}
