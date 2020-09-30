#nullable enable

using System;
using System.Threading.Tasks;
using Windows.System;

namespace Uno.Extensions.System
{
	internal interface ILauncherExtension
    {
		Task<bool> LaunchUriAsync(Uri uri);
		Task<LaunchQuerySupportStatus> QueryUriSupportAsync(Uri uri, LaunchQuerySupportType launchQuerySupportType);
	}
}
