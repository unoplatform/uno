using System.IO;
using Uno.UI.Helpers;
using Uno.UI.RemoteControl;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.Messages;
using Uno.UI.RuntimeTests.Tests.HotReload.Frame.HRApp.Tests;
using Uno.WinUI.Runtime.Skia.X11;
using static Uno.UI.RemoteControl.HotReload.ClientHotReloadProcessor;

namespace Uno.UI.RuntimeTests.Tests.HotReload;

internal static class HotReloadHelper
{
	public static ValueTask<FileUpdate> UpdateAsync(ImmutableArray<FileEdit> edits, CancellationToken ct)
		=> UpdateAsync(new UpdateRequest(edits), ct);

	public static async ValueTask<FileUpdate> UpdateAsync(UpdateRequest request, CancellationToken ct)
	{
		var hr = RemoteControlClient.Instance?.Processors.OfType<ClientHotReloadProcessor>().SingleOrDefault()
			?? throw new InvalidOperationException("Hot-reload not initialized properly");

		request = request.WithExtendedTimeouts(); // Required for CI

		if (await hr.TryUpdateFilesAsync(request, ct) is { Error: { } error })
		{
			throw error;
		}

		return new(hr, request);
	}

	public record FileUpdate(ClientHotReloadProcessor HotReload, UpdateRequest Request) : IAsyncDisposable
	{
		/// <inheritdoc />
		public async ValueTask DisposeAsync()
		{
			var ct = CancellationToken.None; // If test has been aborted, we still want to undo!
			var req = Request.Undo(waitForHotReload: true /* Do not pollute sub-sequent tests */);
			if (await HotReload.TryUpdateFilesAsync(req, ct) is { Error: { } error })
			{
				throw error;
			}
		}
	}

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

		if (message is null)
		{
			return;
		}

		await RemoteControlClient.Instance.SendMessage(message);

		var reloadWaiter = TypeMappings.WaitForResume();
		if (!reloadWaiter.IsCompleted)
		{
			// Reloads are paused (ie task hasn't completed), so don't wait for any update
			// This is to handle testing the pause/resume feature of HR
			return;
		}

		await TestingUpdateHandler.WaitForVisualTreeUpdate().WaitAsync(ct);
	}

	/// <summary>
	/// Updates a specific file using an app relative path.
	/// </summary>
	public static async Task UpdateServerFile(string filePathInProject, string originalText, string replacementText, CancellationToken ct)
	{
		if (RemoteControlClient.Instance is null)
		{
			return;
		}

		await RemoteControlClient.Instance.WaitForConnection();

		var message = new UpdateSingleFileRequest
		{
			FilePath = Path.Combine(GetProjectPath(), filePathInProject.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar)),
			OldText = originalText,
			NewText = replacementText
		};

		if (message is null)
		{
			return;
		}

		await RemoteControlClient.Instance.SendMessage(message);

		var reloadWaiter = TypeMappings.WaitForResume();
		if (!reloadWaiter.IsCompleted)
		{
			// Reloads are paused (ie task hasn't completed), so don't wait for any update
			// This is to handle testing the pause/resume feature of HR
			return;
		}

		await TestingUpdateHandler.WaitForVisualTreeUpdate().WaitAsync(ct);
	}

	/// <summary>
	/// Updates a project file's contents, asserts the change then revert.
	/// </summary>
	public static async Task UpdateProjectFileAndRevert(
		string filePathInProject,
		string originalText,
		string replacementText,
		Func<Task> callback,
		CancellationToken ct)
	{
		if (RemoteControlClient.Instance is null)
		{
			return;
		}

		await RemoteControlClient.Instance.WaitForConnection();

		try
		{
			await UpdateServerFile(filePathInProject, originalText, replacementText, ct);

			await callback();
		}
		finally
		{
			await UpdateServerFile(filePathInProject, replacementText, originalText, CancellationToken.None);
		}
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

		await RemoteControlClient.Instance.WaitForConnection();

		try
		{
			await UpdateServerFile<T>(originalText, replacementText, ct);

			await callback();
		}
		finally
		{
			await UpdateServerFile<T>(replacementText, originalText, CancellationToken.None);
		}
	}

	/// <summary>
	/// Gets the project path from the remotecontrol attributes
	/// </summary>
	private static string GetProjectPath()
	{
		if (Application.Current.GetType().Assembly.GetCustomAttributes(typeof(ProjectConfigurationAttribute), false) is ProjectConfigurationAttribute[] configs)
		{
			var config = configs.First();

			return Path.GetDirectoryName(config.ProjectPath)!;
		}
		else
		{
			throw new InvalidOperationException("Missing ProjectConfigurationAttribute");
		}
	}
}
