using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Tmds.DBus.Protocol;
using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Helpers.WinUI;
using Uno.WinUI.Runtime.Skia.X11.DBus;

namespace Uno.WinUI.Runtime.Skia.X11;

// https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.FileChooser.html
// https://github.com/tmds/Tmds.DBus/blob/main/docs/modelling.md#argument-types

/// <summary>
/// This class implements v3 of the org.freedesktop.portal.FileChooser portal for files as defined by freedesktop.org.
/// v4 is backwards compatible with v3, so this file also implements v4.
/// </summary>
internal class LinuxFilePickerExtension(IFilePicker picker) : IFileOpenPickerExtension, IFolderPickerExtension
{
	private const string Service = "org.freedesktop.portal.Desktop";
	private const string ObjectPath = "/org/freedesktop/portal/desktop";
	private const string ResultObjectPathPrefix = "/org/freedesktop/portal/desktop/request";

	public async Task<StorageFile?> PickSingleFileAsync(CancellationToken token)
		=> await PickFilesAsync(token, false, false)
			.ContinueWith(task => task.Result.Select(StorageFile.GetFileFromPath).FirstOrDefault((StorageFile?)null), token);

	public async Task<IReadOnlyList<StorageFile>> PickMultipleFilesAsync(CancellationToken token)
		=> await PickFilesAsync(token, true, false)
			.ContinueWith(task => task.Result.Select(StorageFile.GetFileFromPath).ToList(), token);

	public async Task<StorageFolder?> PickSingleFolderAsync(CancellationToken token)
		=> await PickFilesAsync(token, false, true)
			.ContinueWith(task => task.Result.Select(path => StorageFolder.GetFolderFromPathAsync(path).GetResults()).FirstOrDefault((StorageFolder?)null), token);

	public async Task<IReadOnlyList<string>> PickFilesAsync(CancellationToken token, bool multiple, bool directory)
	{
		var sessionsAddressBus = Address.Session;
		if (sessionsAddressBus is null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Can not determine DBus session bus address");
			}
			return ImmutableList<string>.Empty;
		}

		if (token.IsCancellationRequested)
		{
			return ImmutableList<string>.Empty;
		}

		using var connection = new Connection(sessionsAddressBus);
		var connectionTcs = new TaskCompletionSource();
		// ConnectAsync calls ConfigureAwait(false), so we need this TCS dance to make the continuation continue on the UI thread
		_ = connection.ConnectAsync().AsTask().ContinueWith(_ => connectionTcs.TrySetResult(), token);
		var timeoutTask = Task.Delay(1000, token);
		var finishedTask = await Task.WhenAny(connectionTcs.Task, timeoutTask);
		if (finishedTask == timeoutTask)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Timed out while trying to connect to DBus");
			}
			return ImmutableList<string>.Empty;
		}

		try
		{
			var desktopService = new DesktopService(connection, Service);
			var chooser = desktopService.CreateFileChooser(ObjectPath);

			if (token.IsCancellationRequested)
			{
				return ImmutableList<string>.Empty;
			}

			var version = await chooser.GetVersionAsync();
			if (version < 3)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"File pickers are only implemented for version 3 and above of the File chooser portal, but version {version} was found");
				}
				return ImmutableList<string>.Empty;
			}

			if (!X11Helper.XamlRootHostFromApplicationView(ApplicationView.GetForCurrentViewSafe(), out var host))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Unable to get the {nameof(X11XamlRootHost)} instance from the application view.");
				}

				return ImmutableList<string>.Empty;
			}

			var handleToken = "UnoFileChooser" + Random.Shared.NextInt64();
			var requestPath = $"{ResultObjectPathPrefix}/{connection.UniqueName![1..].Replace(".", "_")}/{handleToken}";

			if (token.IsCancellationRequested)
			{
				return ImmutableList<string>.Empty;
			}

			// We listen for the signal BEFORE we send the request. The spec API reference
			// points out the race condition that could occur otherwise.
			var request = desktopService.CreateRequest(requestPath);
			var responseTcs = new TaskCompletionSource<(uint Response, Dictionary<string, VariantValue> Results)>();
			_ = request.WatchResponseAsync((exception, tuple) =>
			{
				if (exception is not null)
				{
					responseTcs.SetException(exception);
				}
				else
				{
					responseTcs.SetResult(tuple);
				}
			});

			if (token.IsCancellationRequested)
			{
				return ImmutableList<string>.Empty;
			}

			var actualRequestPath = await chooser.OpenFileAsync(
				parentWindow: "x11:" + host.RootX11Window.Window.ToString("X", CultureInfo.InvariantCulture),
				title: (multiple, directory) switch
				{
					(true, true) => ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_TITLE_DIRECTORY_MULTIPLE"),
					(true, false) => ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_TITLE_FILE_MULTIPLE"),
					(false, true) => ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_TITLE_DIRECTORY_SINGLE"),
					(false, false) => ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_TITLE_FILE_SINGLE")
				},
				options: new Dictionary<string, VariantValue>
				{
					{ "handle_token", handleToken },
					{ "accept_label", string.IsNullOrEmpty(picker.CommitButtonTextInternal) ? ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_ACCEPT_LABEL") : picker.CommitButtonTextInternal },
					{ "multiple", multiple },
					{ "directory", directory },
					{ "filters", GetPortalFilters() },
					{ "current_folder", new Array<byte>(Encoding.UTF8.GetBytes(PickerHelpers.GetInitialDirectory(picker.SuggestedStartLocationInternal)).Append((byte)'\0')) }
				});

			if (actualRequestPath != requestPath)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(chooser.OpenFileAsync)} returned a path '{actualRequestPath}' that is different from supplied handle_token -based path '{requestPath}'");
				}

				return ImmutableList<string>.Empty;
			}

			var (response, results) = await responseTcs.Task;

			if (!Enum.IsDefined(typeof(Response), response))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"FileChooser returned an invalid response number {response}.");
				}

				return ImmutableList<string>.Empty;
			}


			if ((Response)response is not Response.Success)
			{
				if ((Response)response is not Response.UserCancelled && this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"FileChooser's response indicates an unsuccessful operation {(Response)response}");
				}

				return ImmutableList<string>.Empty;
			}

			return results["uris"].GetArray<string>().Select(s => new Uri(s).AbsolutePath).ToImmutableList();
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"DBus FileChooser error, see https://aka.platform.uno/x11-dbus-troubleshoot for troubleshooting information.", e);
			}

			return ImmutableList<string>.Empty;
		}
	}

	private Array<Struct<string, Array<Struct<uint, string>>>> GetPortalFilters()
	{
		// Except from the API reference
		// filters (a(sa(us)))
		//
		// List of serialized file filters.
		//
		// Each item in the array specifies a single filter to offer to the user.
		//
		// The first string is a user-visible name for the filter. The a(us) specifies a list of filter strings, which can be either a glob-style pattern (indicated by 0) or a MIME type (indicated by 1). Patterns are case-sensitive.
		//
		// To match different capitalizations of, e.g. '*.ico', use a pattern like: '*.[iI][cC][oO]'.
		//
		// Example: [('Images', [(0, '*.ico'), (1, 'image/png')]), ('Text', [(0, '*.txt')])]
		//
		// Note that filters are purely there to aid the user in making a useful selection. The portal may still allow the user to select files that don’t match any filter criteria, and applications must be prepared to handle that.

		// We don't have a way to map a filter to a category (e.g. image/png -> Images), so we make every filter its own category
		var list = new Array<Struct<string, Array<Struct<uint, string>>>>();
		foreach (var pattern in picker.FileTypeFilterInternal.Distinct())
		{
			if (pattern is null)
			{
				continue;
			}

			if (pattern == "*")
			{
				list.Add(Struct.Create("All Files", new Array<Struct<uint, string>>(new[] { Struct.Create((uint)0, "*") })));
			}
			else if (pattern.StartsWith('.') && pattern[1..] is var ext && ext.All(char.IsLetterOrDigit))
			{
				list.Add(Struct.Create($"{ext.ToUpperInvariant()} Files", new Array<Struct<uint, string>>(new[] { Struct.Create((uint)0, $"*.{ext}") })));
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Skipping invalid file extension filter: '{pattern}'");
				}
			}
		}

		return list;
	}
}
