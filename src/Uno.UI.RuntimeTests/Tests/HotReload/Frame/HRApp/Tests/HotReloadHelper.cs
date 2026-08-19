using System.IO;
using System.Runtime.CompilerServices;
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
	private const int _timeoutMs = 45_000; // Very long delay for CI, change it to lower value for local tests!

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

	/// <summary>
	/// DO NOT USE, prefer the UpdateAsync method above.
	/// </summary>
	public static async Task UpdateServerFile<T>(string originalText, string replacementText, CancellationToken ct, [CallerMemberName] string caller = "", [CallerLineNumber] int line = -1)
		where T : FrameworkElement, new()
	{
		if (RemoteControlClient.Instance is null)
		{
			return;
		}

		var timeout = Task.Delay(_timeoutMs, ct);
		await Wait(RemoteControlClient.Instance.WaitForConnection(), "connection");

		var message = new T().CreateUpdateFileMessage(
			originalText: originalText,
			replacementText: replacementText);

		if (message is null)
		{
			return;
		}

		EnsureOriginalTextIsPresent(message.FilePath, originalText, caller, line);

		await Wait(RemoteControlClient.Instance.SendMessage(message), "send-msg");

		if (Uno.HotReload.Client.UIUpdate.IsPaused(Uno.HotReload.Client.HotReloadUIPhases.VisualTree))
		{
			// Visual-tree updates are paused (queued for drain on release).
			// The visual tree won't change until the pause is released, so don't
			// wait for an update — the caller is testing the pause feature.
			return;
		}

		await Wait(TestingUpdateHandler.WaitForVisualTreeUpdate().WaitAsync(ct), "tree-update");

		async Task Wait(Task task, string step)
		{
			if (await Task.WhenAny(task, timeout) == timeout)
			{
				throw new TimeoutException($"Timed out waiting for visual tree update from {caller}@{line} <{step}>");
			}
		}
	}

	/// <summary>
	/// DO NOT USE, prefer the UpdateAsync method above.
	/// </summary>
	public static async Task UpdateServerFile(string filePathInProject, string originalText, string replacementText, CancellationToken ct, [CallerMemberName] string caller = "", [CallerLineNumber] int line = -1)
	{
		if (RemoteControlClient.Instance is null)
		{
			return;
		}

		var timeout = Task.Delay(_timeoutMs, ct);
		await Wait(RemoteControlClient.Instance.WaitForConnection(), "connection");

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

		EnsureOriginalTextIsPresent(message.FilePath, originalText, caller, line);

		await Wait(RemoteControlClient.Instance.SendMessage(message), "send-msg");

		if (Uno.HotReload.Client.UIUpdate.IsPaused(Uno.HotReload.Client.HotReloadUIPhases.VisualTree))
		{
			// Visual-tree updates are paused (queued for drain on release).
			// The visual tree won't change until the pause is released, so don't
			// wait for an update — the caller is testing the pause feature.
			return;
		}

		await Wait(TestingUpdateHandler.WaitForVisualTreeUpdate().WaitAsync(ct), "tree-update");

		async Task Wait(Task task, string step)
		{
			if (await Task.WhenAny(task, timeout) == timeout)
			{
				throw new TimeoutException($"Timed out waiting for visual tree update from {caller}@{line} <{step}>");
			}
		}
	}

	/// <summary>
	/// DO NOT USE, prefer the UpdateAsync method above.
	/// </summary>
	public static async Task UpdateProjectFileAndRevert(
		string filePathInProject,
		string originalText,
		string replacementText,
		Func<Task> callback,
		CancellationToken ct,
		[CallerMemberName] string caller = "",
		[CallerLineNumber] int line = -1)
	{
		if (RemoteControlClient.Instance is null)
		{
			return;
		}

		await RemoteControlClient.Instance.WaitForConnection();

		var edited = false;
		try
		{
			await UpdateServerFile(filePathInProject, originalText, replacementText, ct, caller, line);
			edited = true;

			await callback();
		}
		finally
		{
			// Only undo an edit that actually happened: reverting a file that was never modified
			// throws from the same guard and would replace the original failure.
			if (edited)
			{
				await UpdateServerFile(filePathInProject, replacementText, originalText, CancellationToken.None, caller + "<undo>", line);
			}
		}
	}

	/// <summary>
	/// DO NOT USE, prefer the UpdateAsync method above.
	/// </summary>
	public static async Task UpdateServerFileAndRevert<T>(
		string originalText,
		string replacementText,
		Func<Task> callback,
		CancellationToken ct,
		[CallerMemberName] string caller = "",
		[CallerLineNumber] int line = -1)
		where T : FrameworkElement, new()
	{
		if (RemoteControlClient.Instance is null)
		{
			return;
		}

		await RemoteControlClient.Instance.WaitForConnection();

		var edited = false;
		try
		{
			await UpdateServerFile<T>(originalText, replacementText, ct, caller, line);
			edited = true;

			await callback();
		}
		finally
		{
			// Only undo an edit that actually happened: reverting a file that was never modified
			// throws from the same guard and would replace the original failure.
			if (edited)
			{
				await UpdateServerFile<T>(replacementText, originalText, CancellationToken.None, caller + "<undo>", line);
			}
		}
	}

	/// <summary>
	/// Fails fast when <paramref name="originalText"/> is absent from the file the server is about to edit.
	/// </summary>
	/// <remarks>
	/// The replacement is performed server-side and a search string that matches nothing is applied as a
	/// silent no-op: no file change, no hot reload, and a test that then asserts against content which was
	/// never updated. Checking here turns that into an explicit failure naming the file and the caller.
	/// The check is skipped when the source is not readable from this process, leaving the server as the
	/// authority rather than introducing a new failure mode.
	/// </remarks>
	private static void EnsureOriginalTextIsPresent(string filePath, string originalText, string caller, int line)
	{
		if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
		{
			return;
		}

		if (!File.ReadAllText(filePath).Contains(originalText, StringComparison.Ordinal))
		{
			throw new InvalidOperationException(
				$"'{originalText}' was not found in '{filePath}' (requested by {caller}@{line}). " +
				"The edit would have silently done nothing.");
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
