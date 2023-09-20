#nullable enable

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RemoteControl.HotReload.MetadataUpdater;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

internal static class HotReloadHelper
{
	public static async Task UpdateServerFile<T>(string originalText, string replacementText, CancellationToken ct)
		where T : FrameworkElement, new()
	{
		if (RemoteControlClient.Instance is null)
		{
			return;
		}

		await RemoteControlClient.Instance.WaitForConnection();

		var message = new T().CreateUpdateFileMessage(
			originalText: originalText,
			replacementText: replacementText);

		if(message is null)
		{
			return;
		}

		await RemoteControlClient.Instance.SendMessage(message);

		var cts = new TaskCompletionSource();
		ct.Register(() => cts.TrySetCanceled());

		void UpdateReceived(object? sender, object? args) => cts.TrySetResult();

		MetadataUpdaterHelper.MetadataUpdated += UpdateReceived;

		await cts.Task;
	}

	public static async Task UpdateServerFileAndRevert<T>(
		string originalText,
		string replacementText,
		Func<Task> callback,
		CancellationToken ct)
		where T : FrameworkElement, new()
	{
		if (RemoteControlClient.Instance is null)
		{
			return;
		}

		try
		{
			await RemoteControlClient.Instance.WaitForConnection();

			await UpdateServerFile<T>(originalText, replacementText, ct);

			await callback();
		}
		finally
		{
			await UpdateServerFile<T>(replacementText, originalText, ct);
		}
	}
}
