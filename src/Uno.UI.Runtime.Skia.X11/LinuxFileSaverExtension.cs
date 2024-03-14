using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
internal class LinuxFileSaverExtension(FileSavePicker picker) : IFileSavePickerExtension
{
	private const string Service = "org.freedesktop.portal.Desktop";
	private const string ObjectPath = "/org/freedesktop/portal/desktop";
	private const string ResultObjectPathPrefix = "/org/freedesktop/portal/desktop/request";

	public async Task<StorageFile?> PickSaveFileAsync(CancellationToken token)
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

			return await Task.FromResult<StorageFile?>(null);
		}

		var fileChooser = connection.CreateProxy<IFileChooser>(Service, ObjectPath);

		if (fileChooser is null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Unable to find object {ObjectPath} at DBus service {Service}, make sure you have an xdg-desktop-portal implementation installed. For more information, visit https://wiki.archlinux.org/title/XDG_Desktop_Portal#List_of_backends_and_interfaces");
			}

			return await Task.FromResult<StorageFile?>(null);
		}

		if (!X11Helper.XamlRootHostFromApplicationView(ApplicationView.GetForCurrentViewSafe(), out var host))
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Unable to get the {nameof(X11XamlRootHost)} instance from the application view.");
			}

			return await Task.FromResult<StorageFile?>(null);
		}

		var handleToken = "UnoFileChooser" + Random.Shared.Next();
		try
		{
			var requestPath = $"{ResultObjectPathPrefix}/{info.LocalName[1..].Replace(".", "_")}/{handleToken}";

			var result = connection.CreateProxy<IRequest>(Service, requestPath);

			var tcs = new TaskCompletionSource<string?>();

			token.Register(() =>
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"File picker cancelled.");
				}

				tcs.TrySetResult(null);
			});

			// We listen for the signal BEFORE we send the request. The spec API reference
			// points out the race condition that could occur otherwise.
			using var _ = await result.WatchResponseAsync(r =>
			{
				if (r.Response is Response.Success)
				{
					var uris = (IReadOnlyList<string>)r.results["uris"];
					tcs.TrySetResult(uris.Select(uri => new Uri(uri).AbsolutePath).FirstOrDefault((string?)null));
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"File picker received an unsuccessful response {r.Response}.");
					}

					tcs.TrySetResult(null);
				}
			}, e =>
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"File picker failed to receive a response.", e);
				}

				tcs.TrySetResult(null);
			});

			var window = "x11:" + host.X11Window.Window.ToString("X", CultureInfo.InvariantCulture);
			var requestPath2 = await fileChooser.SaveFileAsync(window, ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_TITLE"), new Dictionary<string, object>
			{
				{ "handle_token", handleToken },
				{ "accept_label", string.IsNullOrEmpty(picker.CommitButtonText) ? ResourceAccessor.GetLocalizedStringResource("FILE_SAVER_ACCEPT_LABEL") : picker.CommitButtonText },
				{ "filters", GetPortalFilters(picker.FileTypeChoices) },
				{ "current_name", picker.SuggestedFileName },
				{ "current_folder", Encoding.UTF8.GetBytes(PickerHelpers.GetInitialDirectory(picker.SuggestedStartLocation)).Append((byte)'\0') }
			});

			if (requestPath != requestPath2)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"We are waiting at a wrong path {requestPath} instead of {requestPath2}");
				}

				tcs.TrySetResult(null);
			}

			return await tcs.Task.ContinueWith(task =>
			{
				if (task.Result is { } path)
				{
					try
					{
						File.Create(path).Dispose();
					}
					catch
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error($"Failed to create file at {path}");
						}

						return null;
					}

					return StorageFile.GetFileFromPath(path);
				}

				return null;
			}, token);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to save file", e);
			}

			return await Task.FromResult<StorageFile?>(null);
		}
	}

	private (string, (uint, string)[])[] GetPortalFilters(IDictionary<string, IList<string>> filterDictionary)
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
		var result = new List<(string, (uint, string)[])>();
		foreach (var (category, filters) in filterDictionary)
		{
			result.Add((category,
				filters
					.Select(filter => ((uint)0, $"*{filter}")) // filter should be `.extension`. The portal want a `*.extension` format.
					.ToArray()));
		}

		return result.ToArray();
	}
}
