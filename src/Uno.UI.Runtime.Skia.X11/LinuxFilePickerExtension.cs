using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.ViewManagement;
using Tmds.DBus;
using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Helpers.WinUI;
using Uno.WinUI.Runtime.Skia.X11.Dbus;

namespace Uno.WinUI.Runtime.Skia.X11;

// https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.FileChooser.html
// https://github.com/tmds/Tmds.DBus/blob/main/docs/modelling.md#argument-types

/// <summary>
/// This class implements the org.freedesktop.portal.FileChooser portal for files as defined by freedesktop.org
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
		Connection? connection;
		ConnectionInfo? info;
		try
		{
			connection = Connection.Session;
			info = await connection.ConnectAsync();
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Unable to connect to DBus, see https://aka.platform.uno/x11-dbus-troubleshoot for troubleshooting information.", e);
			}

			return await Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
		}

		var fileChooser = connection.CreateProxy<IFileChooser>(Service, ObjectPath);

		if (fileChooser is null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Unable to find object {ObjectPath} at DBus service {Service}, make sure you have an xdg-desktop-portal implementation installed. For more information, visit https://wiki.archlinux.org/title/XDG_Desktop_Portal#List_of_backends_and_interfaces");
			}

			return await Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
		}

		if (!X11Helper.XamlRootHostFromApplicationView(ApplicationView.GetForCurrentViewSafe(), out var host))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Unable to get the {nameof(X11XamlRootHost)} instance from the application view.");
			}

			return await Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
		}

		var handleToken = "UnoFileChooser" + Random.Shared.Next();
		try
		{
			var requestPath = $"{ResultObjectPathPrefix}/{info.LocalName[1..].Replace(".", "_")}/{handleToken}";

			var result = connection.CreateProxy<IRequest>(Service, requestPath);

			var tcs = new TaskCompletionSource<IReadOnlyList<string>>();

			token.Register(() =>
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"File picker cancelled.");
				}

				tcs.TrySetResult(Array.Empty<string>());
			});

			// We listen for the signal BEFORE we send the request. The spec API reference
			// points out the race condition that could occur otherwise.
			using var _ = await result.WatchResponseAsync(r =>
			{
				if (r.Response is Response.Success)
				{
					tcs.TrySetResult(((IReadOnlyList<string>)r.results["uris"]).Select(s => new Uri(s).AbsolutePath).ToList());
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"File picker received an unsuccessful response {r.Response}.");
					}

					tcs.TrySetResult(Array.Empty<string>());
				}
			}, e =>
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"File picker failed to receive a response.", e);
				}

				tcs.TrySetResult(Array.Empty<string>());
			});

			var title = (multiple, directory) switch
			{
				(true, true) => ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_TITLE_DIRECTORY_MULTIPLE"),
				(true, false) => ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_TITLE_FILE_MULTIPLE"),
				(false, true) => ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_TITLE_DIRECTORY_SINGLE"),
				(false, false) => ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_TITLE_FILE_SINGLE")
			};
			var window = "x11:" + host.X11Window.Window.ToString("X", CultureInfo.InvariantCulture);
			var requestPath2 = await fileChooser.OpenFileAsync(window, title, new Dictionary<string, object>
			{
				{ "handle_token", handleToken },
				{ "accept_label", string.IsNullOrEmpty(picker.CommitButtonText) ? ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_ACCEPT_LABEL") : picker.CommitButtonText },
				{ "multiple", multiple },
				{ "directory", directory },
				{ "filters", GetPortalFilters(picker.FileTypeFilter) },
				{ "current_folder", Encoding.UTF8.GetBytes(PickerHelpers.GetInitialDirectory(picker.SuggestedStartLocation)).Append((byte)'\0') }
			});

			if (requestPath != requestPath2)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"We are waiting at a wrong path {requestPath} instead of {requestPath2}");
				}

				tcs.TrySetResult(Array.Empty<string>());
			}

			return await tcs.Task;
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to pick file", e);
			}

			return await Task.FromResult((IReadOnlyList<string>)Array.Empty<string>());
		}
	}

	private (string, (uint, string)[])[] GetPortalFilters(IList<string> filters)
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
		//
		var list = new List<(uint kind, string pattern)>();
		foreach (var filter in filters)
		{
			try
			{
				// will throw if not a valid MIME type
				list.Add((1, new System.Net.Mime.ContentType(filter).ToString()));
			}
			catch (Exception)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Failed to parse portal filer {filter} as a MIME type, falling back to glob.");
				}
				// The portal accepts any glob pattern, but to be similar to other platforms, we
				// assume the filter is of the form `.extension`.
				if (filter == "*")
				{
					list.Add((0, filter));
				}
				else
				{
					list.Add((0, $"*{filter}"));
				}
			}
		}

		// We don't have a way to map a filter to a category (e.g. image/png -> Images), so we make every filter its own category
		return list.Select(f => (f.pattern, new[] { f })).ToArray();
	}
}
