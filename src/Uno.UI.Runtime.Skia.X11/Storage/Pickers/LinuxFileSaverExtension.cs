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
using Tmds.DBus.Protocol;
using Uno.Extensions.Storage.Pickers;
using Uno.Foundation.Logging;
using Uno.UI.Helpers;
using Uno.UI.Helpers.WinUI;
using Uno.WinUI.Runtime.Skia.X11.DBus;

namespace Uno.WinUI.Runtime.Skia.X11;

// https://flatpak.github.io/xdg-desktop-portal/docs/doc-org.freedesktop.portal.FileChooser.html

/// <summary>
/// This class implements v4 of the org.freedesktop.portal.FileChooser portal for files as defined by freedesktop.org
/// v4 is backwards compatible with v3, so this file also implements v4.
/// </summary>
internal class LinuxFileSaverExtension(FileSavePicker picker) : IFileSavePickerExtension
{
	private const string Service = "org.freedesktop.portal.Desktop";
	private const string ObjectPath = "/org/freedesktop/portal/desktop";
	private const string ResultObjectPathPrefix = "/org/freedesktop/portal/desktop/request";

	public async Task<StorageFile?> PickSaveFileAsync(CancellationToken token)
	{
		var sessionsAddressBus = DBusAddress.Session;
		if (sessionsAddressBus is null)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Can not determine DBus session bus address");
			}
			return null;
		}

		if (token.IsCancellationRequested)
		{
			return null;
		}

		using var connection = new DBusConnection(sessionsAddressBus);
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
			return null;
		}

		try
		{
			var desktopService = new DBusService(connection, Service);
			var chooser = desktopService.CreateFileChooser(ObjectPath);

			if (token.IsCancellationRequested)
			{
				return null;
			}

			var version = await chooser.GetVersionAsync();
			if (version < 3)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"File pickers are only implemented for version 3 and above of the File chooser portal, but version {version} was found");
				}
				return null;
			}

			if (!X11Helper.XamlRootHostFromApplicationView(ApplicationView.GetForCurrentViewSafe(), out var host))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Unable to get the {nameof(X11XamlRootHost)} instance from the application view.");
				}

				return null;
			}

			var handleToken = "UnoFileChooser" + Random.Shared.NextInt64();
			var requestPath = $"{ResultObjectPathPrefix}/{connection.UniqueName![1..].Replace(".", "_")}/{handleToken}";

			if (token.IsCancellationRequested)
			{
				return null;
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
				return null;
			}

			var actualRequestPath = await chooser.SaveFileAsync(
				parentWindow: "x11:" + host.RootX11Window.Window.ToString("X", CultureInfo.InvariantCulture),
				title: ResourceAccessor.GetLocalizedStringResource("FILE_PICKER_TITLE"),
				options: new Dictionary<string, VariantValue>
				{
					{ "handle_token", handleToken },
					{ "accept_label", string.IsNullOrEmpty(picker.CommitButtonText) ? ResourceAccessor.GetLocalizedStringResource("FILE_SAVER_ACCEPT_LABEL") : picker.CommitButtonText },
					{ "filters", GetPortalFilters(picker.FileTypeChoices) },
					{ "current_name", picker.SuggestedFileName },
					{ "current_folder", new Array<byte>(Encoding.UTF8.GetBytes(PickerHelpers.GetInitialDirectory(picker.SuggestedStartLocation)).Append((byte)'\0')) }
				});

			if (actualRequestPath != requestPath)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"{nameof(chooser.OpenFileAsync)} returned a path '{actualRequestPath}' that is different from supplied handle_token -based path '{requestPath}'");
				}

				return null;
			}

			var (response, results) = await responseTcs.Task;

			if (!Enum.IsDefined((Response)response))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"FileChooser returned an invalid response number {response}.");
				}

				return null;
			}

			if ((Response)response is not Response.Success)
			{
				if ((Response)response is not Response.UserCancelled && this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"FileChooser's response indicates an unsuccessful operation {(Response)response}");
				}

				return null;
			}

			return StorageFile.GetFileFromPath(new Uri(results["uris"].GetArray<string>()[0]).AbsolutePath);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"DBus FileChooser error, see https://aka.platform.uno/x11-dbus-troubleshoot for troubleshooting information.", e);
			}

			return null;
		}
	}

	private Array<Struct<string, Array<Struct<uint, string>>>> GetPortalFilters(IDictionary<string, IList<string>> filterDictionary)
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

		var list = new Array<Struct<string, Array<Struct<uint, string>>>>();
		foreach (var (category, filters) in filterDictionary)
		{
			var adjustedFilters = filters
				.Where(pattern => pattern.StartsWith('.') && pattern[1..] is var ext && ext.All(char.IsLetterOrDigit))
				.Select(pattern => Struct.Create((uint)0, $"*{pattern}")); // pattern should be `.extension`. The portal wants a `*.extension` format.
			list.Add(Struct.Create(category, new Array<Struct<uint, string>>(adjustedFilters)));
		}

		return list;
	}
}
