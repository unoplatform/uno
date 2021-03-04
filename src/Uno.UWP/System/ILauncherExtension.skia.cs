#nullable enable

using System;
using System.Threading.Tasks;

namespace Uno.Extensions.System
{
	internal interface ILauncherExtension
    {
		Task<bool> LaunchUriAsync(Uri uri);
	}
}
