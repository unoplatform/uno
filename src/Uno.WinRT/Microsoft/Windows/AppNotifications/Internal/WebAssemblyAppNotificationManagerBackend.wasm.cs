#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.Helpers.Serialization;

namespace Microsoft.Windows.AppNotifications.Internal;

internal sealed partial class WebAssemblyAppNotificationManagerBackend : IAppNotificationManagerBackend, IDeferredAppNotificationManagerBackend, IAsyncAppNotificationManagerBackend, IAppNotificationProgressUpdateCapability, IAppNotificationActiveIdRefreshCapability
{
	private const string NativeTagPrefix = "uno.appnotifications.";
	private static readonly object _pendingShowsGate = new();
	private static readonly Dictionary<string, PendingShow> _pendingShows = new(StringComparer.Ordinal);
	private readonly bool _useServiceWorker = WebAssemblyAppNotificationConfiguration.UseServiceWorker;
	private Action<string, uint, bool>? _showCompleted;
	private bool _isRegistered;

	public bool IsSupported => NativeMethods.IsSupported(_useServiceWorker);

	public bool DefersShowCompletion => _useServiceWorker;

	public bool SupportsProgressUpdates => false;

	public bool RequiresActiveIdsForStateChanges => _useServiceWorker;

	public AppNotificationSetting Setting
		=> WebAssemblyAppNotificationSettingEvaluator.Evaluate(IsSupported, NativeMethods.GetPermission());

	public string? BootIdentifier => null;

	public void Register()
	{
		if (!_isRegistered)
		{
			NativeMethods.Initialize(_useServiceWorker);
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

	public void UnregisterAll()
	{
		Unregister();
		if (_useServiceWorker)
		{
			NativeMethods.UnregisterAll(NativeTagPrefix);
		}
	}

	public bool TryShow(AppNotificationEnvelope notification)
		=> TryPost(WebAssemblyAppNotificationTranslator.Translate(notification), Guid.NewGuid().ToString("N"));

	public bool TryShow(AppNotificationEnvelope notification, string operationCorrelation)
		=> TryPost(WebAssemblyAppNotificationTranslator.Translate(notification), operationCorrelation);

	public bool TryUpdate(AppNotificationStateRecord notification)
		=> TryPost(WebAssemblyAppNotificationTranslator.Translate(notification.ToEnvelope()), Guid.NewGuid().ToString("N"));

	public bool TryUpdate(AppNotificationStateRecord notification, string operationCorrelation)
		=> TryPost(WebAssemblyAppNotificationTranslator.Translate(notification.ToEnvelope()), operationCorrelation);

	public void Remove(AppNotificationStateRecord notification)
		=> NativeMethods.Close(NativeTagPrefix + notification.Id);

	public void RemoveAll() => NativeMethods.CloseAll(NativeTagPrefix);

	public IReadOnlyCollection<uint>? GetActiveNotificationIds()
		=> _useServiceWorker ? null : ParseActiveIds(NativeMethods.GetActiveIds(NativeTagPrefix));

	public Task<bool> TryUpdateAsync(AppNotificationStateRecord notification)
		=> TryPostAsync(WebAssemblyAppNotificationTranslator.Translate(notification.ToEnvelope()));

	public Task<bool> RemoveAsync(AppNotificationStateRecord notification)
		=> NativeMethods.CloseAsync(NativeTagPrefix + notification.Id);

	public Task<bool> RemoveAllAsync()
		=> NativeMethods.CloseAllAsync(NativeTagPrefix);

	public async Task<IReadOnlyCollection<uint>?> GetActiveNotificationIdsAsync()
		=> ParseActiveIds(await NativeMethods.GetActiveIdsAsync(NativeTagPrefix));

	private static IReadOnlyCollection<uint>? ParseActiveIds(string? value)
	{
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

	public void SetShowCompletedHandler(Action<string, uint, bool> handler)
		=> _showCompleted = handler ?? throw new ArgumentNullException(nameof(handler));

	public bool IsShowPending(uint id)
	{
		lock (_pendingShowsGate)
		{
			return _pendingShows.Values.Any(pending => pending.Id == id);
		}
	}

	public Task WaitForPendingShowsAsync()
	{
		lock (_pendingShowsGate)
		{
			return _pendingShows.Count == 0
				? Task.CompletedTask
				: Task.WhenAll(_pendingShows.Values.Select(pending => pending.Completion.Task));
		}
	}

	internal static bool CompleteShow(string operationCorrelation, uint id, bool succeeded)
	{
		PendingShow? pending;
		lock (_pendingShowsGate)
		{
			if (!_pendingShows.TryGetValue(operationCorrelation, out pending) || pending.Id != id)
			{
				return false;
			}
			_pendingShows.Remove(operationCorrelation);
		}
		try
		{
			pending.Owner._showCompleted?.Invoke(operationCorrelation, id, succeeded);
		}
		finally
		{
			pending.Completion.TrySetResult();
		}
		return true;
	}

	private bool TryPost(WebAssemblyAppNotificationCommand command, string operationCorrelation)
	{
		NativeMethods.InitializePosting(_useServiceWorker);
		if (NativeMethods.GetPermission() != "granted")
		{
			return false;
		}

		if (_useServiceWorker)
		{
			ArgumentException.ThrowIfNullOrEmpty(operationCorrelation);
			lock (_pendingShowsGate)
			{
				_pendingShows.Add(
					operationCorrelation,
					new PendingShow(this, command.Id, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)));
			}
		}
		bool posted;
		try
		{
			posted = NativeMethods.Show(
				JsonHelper.Serialize(command, WebAssemblyAppNotificationSerializationContext.Default),
				operationCorrelation);
		}
		catch
		{
			RemovePendingShow(operationCorrelation);
			throw;
		}
		if (!posted)
		{
			RemovePendingShow(operationCorrelation);
		}
		if (command.UnsupportedFeatures.Length > 0 && typeof(WebAssemblyAppNotificationManagerBackend).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(WebAssemblyAppNotificationManagerBackend).Log().LogWarning(
				$"Web app notifications do not support {string.Join(", ", command.UnsupportedFeatures)}; those features were ignored.");
		}
		return posted;
	}

	private async Task<bool> TryPostAsync(WebAssemblyAppNotificationCommand command)
	{
		NativeMethods.InitializePosting(_useServiceWorker);
		if (NativeMethods.GetPermission() != "granted")
		{
			return false;
		}
		return await NativeMethods.ShowAsync(
			JsonHelper.Serialize(command, WebAssemblyAppNotificationSerializationContext.Default),
			Guid.NewGuid().ToString("N"));
	}

	private void RemovePendingShow(string operationCorrelation)
	{
		if (!_useServiceWorker)
		{
			return;
		}
		PendingShow? pending;
		lock (_pendingShowsGate)
		{
			_pendingShows.Remove(operationCorrelation, out pending);
		}
		pending?.Completion.TrySetResult();
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
		internal static partial bool IsSupported(bool useServiceWorker);

		[JSImport($"{JsType}.getPermission")]
		internal static partial string GetPermission();

		[JSImport($"{JsType}.initialize")]
		internal static partial void Initialize(bool useServiceWorker);

		[JSImport($"{JsType}.initializePosting")]
		internal static partial void InitializePosting(bool useServiceWorker);

		[JSImport($"{JsType}.uninitialize")]
		internal static partial void Uninitialize();

		[JSImport($"{JsType}.requestPermission")]
		internal static partial void RequestPermission();

		[JSImport($"{JsType}.show")]
		internal static partial bool Show(string commandJson, string operationCorrelation);

		[JSImport($"{JsType}.showAsync")]
		internal static partial Task<bool> ShowAsync(string commandJson, string operationCorrelation);

		[JSImport($"{JsType}.close")]
		internal static partial void Close(string tag);

		[JSImport($"{JsType}.closeAsync")]
		internal static partial Task<bool> CloseAsync(string tag);

		[JSImport($"{JsType}.closeAll")]
		internal static partial void CloseAll(string tagPrefix);

		[JSImport($"{JsType}.closeAllAsync")]
		internal static partial Task<bool> CloseAllAsync(string tagPrefix);

		[JSImport($"{JsType}.unregisterAll")]
		internal static partial void UnregisterAll(string tagPrefix);

		[JSImport($"{JsType}.getActiveIds")]
		internal static partial string? GetActiveIds(string tagPrefix);

		[JSImport($"{JsType}.getActiveIdsAsync")]
		internal static partial Task<string?> GetActiveIdsAsync(string tagPrefix);
	}

	private sealed record PendingShow(
		WebAssemblyAppNotificationManagerBackend Owner,
		uint Id,
		TaskCompletionSource Completion);
}

internal static partial class WebAssemblyAppNotificationActivation
{
	[JSExport]
	internal static int Dispatch(string argument)
		=> AppNotificationActivationBroker.Publish(new AppNotificationActivation(argument ?? string.Empty, new Dictionary<string, string>())) ? 0 : -1;

	[JSExport]
	internal static int DispatchShowResult(string operationCorrelation, int id, bool succeeded)
	{
		if (string.IsNullOrEmpty(operationCorrelation) ||
			id <= 0)
		{
			return -1;
		}
		return WebAssemblyAppNotificationManagerBackend.CompleteShow(operationCorrelation, (uint)id, succeeded) ? 0 : -1;
	}
}