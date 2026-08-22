#nullable enable

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Uno.Extensions.System
{
	internal interface ILauncherExtension
	{
		Task<bool> LaunchUriAsync(Uri uri);
		Task<LaunchQuerySupportStatus> QueryUriSupportAsync(Uri uri, LaunchQuerySupportType launchQuerySupportType);

		Task<bool> LaunchFileAsync(IStorageFile file)
		{
			try
			{
				var processStartInfo = new ProcessStartInfo(file.Path)
				{
					UseShellExecute = true,
				};

				var process = new Process();
				process.StartInfo = processStartInfo;
				return Task.FromResult(process.Start());
			}
			catch (Exception)
			{
				return Task.FromResult(false);
			}
		}
	}
}
