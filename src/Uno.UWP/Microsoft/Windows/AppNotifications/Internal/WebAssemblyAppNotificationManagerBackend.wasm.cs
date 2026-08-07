#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json.Serialization;
using Uno.Foundation.Logging;
using Uno.Helpers.Serialization;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed partial class WebAssemblyAppNotificationManagerBackend : IAppNotificationManagerBackend
{
	private const string NativeTagPrefix = "uno.appnotifications.";
	private bool _isRegistered;

	public bool IsSupported => NativeMethods.IsSupported();

	public AppNotificationSetting Setting
		=> WebAssemblyAppNotificationSettingEvaluator.Evaluate(IsSupported, NativeMethods.GetPermission());

	public string? BootIdentifier => null;

	public void Register()
	{
		if (!_isRegistered)
		{
			NativeMethods.Initialize();
			NativeMethods.RequestPermission();
			_isRegistered = true;
		}
	}

	public void Register(string displayName, Uri iconUri) => Register();

	public void Unregister()
	{
		if (_isRegistered)
		{
			NativeMethods.Uninitialize();
			_isRegistered = false;
		}
	}

	public void UnregisterAll() => Unregister();

	public bool TryShow(AppNotificationEnvelope notification)
		=> TryPost(WebAssemblyAppNotificationTranslator.Translate(notification));

	public bool TryUpdate(AppNotificationStateRecord notification)
		=> TryPost(WebAssemblyAppNotificationTranslator.Translate(notification.ToEnvelope()));

	public void Remove(AppNotificationStateRecord notification)
		=> NativeMethods.Close(NativeTagPrefix + notification.Id);

	public void RemoveAll() => NativeMethods.CloseAll(NativeTagPrefix);

	public IReadOnlyCollection<uint>? GetActiveNotificationIds()
	{
		var value = NativeMethods.GetActiveIds(NativeTagPrefix);
		if (value is null)
		{
			return null;
		}
		if (value.Length == 0)
		{
			return Array.Empty<uint>();
		}

		return value
			.Split(',', StringSplitOptions.RemoveEmptyEntries)
			.Select(item => uint.TryParse(item, out var id) ? id : 0)
			.Where(id => id != 0)
			.Distinct()
			.ToArray();
	}

	private static bool TryPost(WebAssemblyAppNotificationCommand command)
	{
		if (NativeMethods.GetPermission() != "granted")
		{
			return false;
		}

		var posted = NativeMethods.Show(JsonHelper.Serialize(command, WebAssemblyAppNotificationSerializationContext.Default));
		if (command.UnsupportedFeatures.Length > 0 && typeof(WebAssemblyAppNotificationManagerBackend).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(WebAssemblyAppNotificationManagerBackend).Log().LogWarning(
				$"Web app notifications do not support {string.Join(", ", command.UnsupportedFeatures)}; those features were ignored.");
		}
		return posted;
	}

	[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
	[JsonSerializable(typeof(WebAssemblyAppNotificationCommand))]
	[JsonSerializable(typeof(WebAssemblyAppNotificationActionCommand[]))]
	internal partial class WebAssemblyAppNotificationSerializationContext : JsonSerializerContext
	{
	}

	private static partial class NativeMethods
	{
		private const string JsType = "globalThis.Windows.UI.Notifications.AppNotificationManager";

		[JSImport($"{JsType}.isSupported")]
		internal static partial bool IsSupported();

		[JSImport($"{JsType}.getPermission")]
		internal static partial string GetPermission();

		[JSImport($"{JsType}.initialize")]
		internal static partial void Initialize();

		[JSImport($"{JsType}.uninitialize")]
		internal static partial void Uninitialize();

		[JSImport($"{JsType}.requestPermission")]
		internal static partial void RequestPermission();

		[JSImport($"{JsType}.show")]
		internal static partial bool Show(string commandJson);

		[JSImport($"{JsType}.close")]
		internal static partial void Close(string tag);

		[JSImport($"{JsType}.closeAll")]
		internal static partial void CloseAll(string tagPrefix);

		[JSImport($"{JsType}.getActiveIds")]
		internal static partial string? GetActiveIds(string tagPrefix);
	}
}

internal static partial class WebAssemblyAppNotificationActivation
{
	[JSExport]
	internal static int Dispatch(string argument)
		=> AppNotificationActivationBroker.Publish(new AppNotificationActivation(argument ?? string.Empty, new Dictionary<string, string>())) ? 0 : -1;
}