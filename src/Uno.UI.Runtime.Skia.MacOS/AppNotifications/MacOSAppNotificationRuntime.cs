#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;
using Uno.Foundation.Logging;
using Windows.ApplicationModel;
using Windows.UI.Notifications.Internal;

namespace Uno.UI.Runtime.Skia.MacOS;

internal static partial class MacOSAppNotificationRuntime
{
	private static readonly object _gate = new();
	private static bool _isInitialized;

	public static unsafe bool Initialize()
	{
		lock (_gate)
		{
			if (_isInitialized)
			{
				return true;
			}
			NativeUno.uno_notifications_set_callbacks(&OnActivated, &OnDelivered);
			if (!NativeUno.uno_notifications_initialize())
			{
				return false;
			}
			_isInitialized = true;
			return true;
		}
	}

	public static bool IsSupported
	{
		get
		{
			Initialize();
			return NativeUno.uno_notifications_is_supported();
		}
	}

	public static AppNotificationSetting Setting
	{
		get
		{
			if (!Initialize())
			{
				return AppNotificationSetting.DisabledForApplication;
			}
			return NativeUno.uno_notifications_get_setting() switch
			{
				2 or 3 => AppNotificationSetting.Enabled,
				1 => AppNotificationSetting.DisabledForApplication,
				5 => AppNotificationSetting.Unsupported,
				_ => AppNotificationSetting.DisabledForApplication,
			};
		}
	}

	public static void RequestAuthorization()
	{
		Initialize();
		NativeUno.uno_notifications_request_authorization();
	}

	public static bool TryPost(AppleAppNotificationCommand command, TimeSpan? delay = null)
	{
		if (!Initialize())
		{
			return false;
		}
		var nativeCommand = ResolveAttachment(AppleAppNotificationTranslator.PrepareForPosting(command));
		var json = JsonSerializer.Serialize(
			nativeCommand,
			typeof(AppleAppNotificationCommand),
			MacOSAppNotificationSerializationContext.Default);
		return NativeUno.uno_notifications_post(json, Math.Max(0d, delay?.TotalSeconds ?? 0d));
	}

	public static bool Remove(string requestIdentifier)
	{
		Initialize();
		return NativeUno.uno_notifications_remove(requestIdentifier);
	}

	public static bool RemoveNotification(uint id)
		=> Remove(AppleAppNotificationTranslator.GetNotificationRequestIdentifier(id)) &&
			RemoveAll(AppleAppNotificationTranslator.GetNotificationRequestIdentifierPrefix(id));

	public static bool RemoveScheduled(string scheduleIdentifier)
		=> Remove(AppleAppNotificationTranslator.GetScheduledRequestIdentifier(scheduleIdentifier)) &&
			RemoveAll(AppleAppNotificationTranslator.GetScheduledRequestIdentifierPrefix(scheduleIdentifier));

	public static bool RemoveAll(string requestIdentifierPrefix)
	{
		Initialize();
		return NativeUno.uno_notifications_remove_all(requestIdentifierPrefix);
	}

	public static IReadOnlyCollection<uint>? GetActiveNotificationIds()
	{
		var identifiers = GetIdentifiers(AppleAppNotificationTranslator.RequestIdentifierPrefix, includePending: true, includeDelivered: true);
		return identifiers?
			.Select(identifier => AppleAppNotificationTranslator.TryGetNotificationId(identifier, out var id) ? id : 0)
			.Where(id => id != 0)
			.Distinct()
			.ToArray();
	}

	public static IReadOnlyCollection<string>? GetPendingScheduleIdentifiers()
		=> GetScheduleIdentifiers(includePending: true, includeDelivered: false);

	public static IReadOnlyCollection<string>? GetDeliveredScheduleIdentifiers()
		=> GetScheduleIdentifiers(includePending: false, includeDelivered: true);

	private static IReadOnlyCollection<string>? GetScheduleIdentifiers(bool includePending, bool includeDelivered)
		=> GetIdentifiers(AppleAppNotificationTranslator.ScheduledRequestIdentifierPrefix, includePending, includeDelivered)?
			.Select(identifier => AppleAppNotificationTranslator.TryGetScheduleIdentifier(identifier, out var scheduleIdentifier) ? scheduleIdentifier : string.Empty)
			.Where(identifier => identifier.Length > 0)
			.Distinct(StringComparer.Ordinal)
			.ToArray();

	private static string[]? GetIdentifiers(string prefix, bool includePending, bool includeDelivered)
	{
		Initialize();
		var pointer = NativeUno.uno_notifications_get_identifiers_json(prefix, includePending, includeDelivered);
		if (pointer == 0)
		{
			return null;
		}
		try
		{
			var json = Marshal.PtrToStringUTF8(pointer);
			return json is null
				? null
				: (string[]?)JsonSerializer.Deserialize(
					json,
					typeof(string[]),
					MacOSAppNotificationSerializationContext.Default);
		}
		finally
		{
			NativeUno.uno_notifications_free_string(pointer);
		}
	}

	private static AppleAppNotificationCommand ResolveAttachment(AppleAppNotificationCommand command)
	{
		var installedPath = command.AttachmentSource.StartsWith("ms-appx:", StringComparison.OrdinalIgnoreCase)
			? Package.Current.InstalledPath
			: string.Empty;
		if (!AppleAppNotificationAssetPathResolver.TryResolve(
			command.AttachmentSource,
			installedPath,
			out var path))
		{
			return command with { AttachmentSource = string.Empty };
		}
		return command with { AttachmentSource = File.Exists(path) ? path : string.Empty };
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnActivated(byte* requestIdentifier, byte* argument, byte* protocolUri, byte* inputId, byte* userText)
	{
		try
		{
			var parsedProtocolUri = ToString(protocolUri);
			if (parsedProtocolUri.Length > 0 && Uri.TryCreate(parsedProtocolUri, UriKind.Absolute, out var uri))
			{
				_ = Windows.System.Launcher.LaunchUriPlatformAsync(uri);
				return;
			}

			var userInput = new Dictionary<string, string>();
			var parsedInputId = ToString(inputId);
			if (parsedInputId.Length > 0)
			{
				userInput[parsedInputId] = ToString(userText);
			}
			AppNotificationActivationBroker.Publish(new AppNotificationActivation(ToString(argument), userInput));
		}
		catch (Exception exception)
		{
			LogCallbackError("macOS app-notification activation failed.", exception);
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static unsafe void OnDelivered(byte* requestIdentifier)
	{
		try
		{
			if (AppleAppNotificationTranslator.TryGetScheduleIdentifier(ToString(requestIdentifier), out var scheduleIdentifier))
			{
				ToastNotificationSchedulerRuntime.CompleteNativeDelivery(scheduleIdentifier);
			}
		}
		catch (Exception exception)
		{
			LogCallbackError("macOS scheduled-notification completion failed.", exception);
		}
	}

	private static void LogCallbackError(string message, Exception exception)
	{
		if (typeof(MacOSAppNotificationRuntime).Log().IsEnabled(LogLevel.Error))
		{
			typeof(MacOSAppNotificationRuntime).Log().Error(message, exception);
		}
	}

	private static unsafe string ToString(byte* value)
		=> value is null ? string.Empty : Marshal.PtrToStringUTF8((nint)value) ?? string.Empty;

	[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(AppleAppNotificationCommand))]
	[JsonSerializable(typeof(AppleAppNotificationActionCommand[]))]
	[JsonSerializable(typeof(string[]))]
	private partial class MacOSAppNotificationSerializationContext : JsonSerializerContext
	{
	}
}