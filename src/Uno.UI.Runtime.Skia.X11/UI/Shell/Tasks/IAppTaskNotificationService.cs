#nullable enable

using System;
using System.Threading.Tasks;

namespace Uno.WinUI.Runtime.Skia.X11;

internal readonly record struct AppTaskNotificationSupport(bool IsSupported, string? Owner);

internal interface IAppTaskNotificationService : IDisposable
{
	Task<AppTaskNotificationSupport> ProbeAsync();

	Task<string> GetOwnerAsync();

	Task<uint> NotifyAsync(string owner, uint replacesId, string icon, string summary, string body);

	Task CloseAsync(string owner, uint notificationId);

	void Reset();
}
