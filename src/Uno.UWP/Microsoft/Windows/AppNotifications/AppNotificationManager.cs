#nullable enable

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.Windows.AppNotifications;

[Uno.NotImplemented]
public sealed class AppNotificationManager
{
	private const string TypeName = "Microsoft.Windows.AppNotifications.AppNotificationManager";
	private static readonly AppNotificationManager _default = new();

	private AppNotificationManager()
	{
	}

	public static AppNotificationManager Default => _default;

	public AppNotificationSetting Setting => AppNotificationSetting.Unsupported;

	public static bool IsSupported() => false;

	[Uno.NotImplemented]
	public void Register()
		=> ApiInformation.TryRaiseNotImplemented(TypeName, "Register()");

	[Uno.NotImplemented]
	public void Register(string displayName, Uri iconUri)
		=> ApiInformation.TryRaiseNotImplemented(TypeName, "Register(string displayName, Uri iconUri)");

	[Uno.NotImplemented]
	public void Unregister()
		=> ApiInformation.TryRaiseNotImplemented(TypeName, "Unregister()");

	[Uno.NotImplemented]
	public void UnregisterAll()
		=> ApiInformation.TryRaiseNotImplemented(TypeName, "UnregisterAll()");

	[Uno.NotImplemented]
	public void Show(AppNotification notification)
		=> ApiInformation.TryRaiseNotImplemented(TypeName, "Show(AppNotification notification)");

	[Uno.NotImplemented]
	public IAsyncOperation<AppNotificationProgressResult> UpdateAsync(AppNotificationProgressData data, string tag, string group)
		=> throw ApiInformation.CreateNotImplementedException(TypeName, "UpdateAsync(AppNotificationProgressData data, string tag, string group)");

	[Uno.NotImplemented]
	public IAsyncOperation<AppNotificationProgressResult> UpdateAsync(AppNotificationProgressData data, string tag)
		=> throw ApiInformation.CreateNotImplementedException(TypeName, "UpdateAsync(AppNotificationProgressData data, string tag)");

	[Uno.NotImplemented]
	public IAsyncAction RemoveByIdAsync(uint notificationId)
		=> throw ApiInformation.CreateNotImplementedException(TypeName, "RemoveByIdAsync(uint notificationId)");

	[Uno.NotImplemented]
	public IAsyncAction RemoveByTagAsync(string tag)
		=> throw ApiInformation.CreateNotImplementedException(TypeName, "RemoveByTagAsync(string tag)");

	[Uno.NotImplemented]
	public IAsyncAction RemoveByTagAndGroupAsync(string tag, string group)
		=> throw ApiInformation.CreateNotImplementedException(TypeName, "RemoveByTagAndGroupAsync(string tag, string group)");

	[Uno.NotImplemented]
	public IAsyncAction RemoveByGroupAsync(string group)
		=> throw ApiInformation.CreateNotImplementedException(TypeName, "RemoveByGroupAsync(string group)");

	[Uno.NotImplemented]
	public IAsyncAction RemoveAllAsync()
		=> throw ApiInformation.CreateNotImplementedException(TypeName, "RemoveAllAsync()");

	[Uno.NotImplemented]
	public IAsyncOperation<IList<AppNotification>> GetAllAsync()
		=> throw ApiInformation.CreateNotImplementedException(TypeName, "GetAllAsync()");

	[Uno.NotImplemented]
	public event TypedEventHandler<AppNotificationManager, AppNotificationActivatedEventArgs> NotificationInvoked
	{
		add => ApiInformation.TryRaiseNotImplemented(TypeName, "event NotificationInvoked");
		remove => ApiInformation.TryRaiseNotImplemented(TypeName, "event NotificationInvoked");
	}
}
