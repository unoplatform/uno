#nullable enable
#pragma warning disable CS8305

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Shell.Tasks;
using Windows.UI.Shell.Tasks;

namespace Uno.WinUI.Runtime.Skia.X11;

internal sealed class X11AppTaskInfoExtension : AppTaskInfoExtensionBase, IDisposable
{
	private static readonly X11AppTaskInfoExtension Instance = new();

	private readonly IAppTaskNotificationService _service;
	private readonly long _probeIntervalMilliseconds;
	private readonly Timer? _supportTimer;
	private readonly Dictionary<string, string> _signatures = new(StringComparer.Ordinal);
	private readonly Dictionary<string, uint> _notificationIds = new(StringComparer.Ordinal);
	private readonly object _supportProbeGate = new();
	private Task? _supportProbe;
	private int _supportState;
	private int _isDisposed;
	private string? _supportOwner;
	private string? _publishedOwner;
	private long _nextSupportProbeAt;

	private X11AppTaskInfoExtension()
		: this(new DBusAppTaskNotificationService(), TimeSpan.FromSeconds(5))
	{
	}

	internal X11AppTaskInfoExtension(IAppTaskNotificationService service, TimeSpan probeInterval)
	{
		ArgumentNullException.ThrowIfNull(service);
		if (probeInterval < TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(probeInterval));
		}

		_service = service;
		_probeIntervalMilliseconds = (long)probeInterval.TotalMilliseconds;
		if (probeInterval > TimeSpan.Zero)
		{
			_supportTimer = new Timer(
				static state => ((X11AppTaskInfoExtension)state!).EnsureSupportProbe(),
				this,
				probeInterval,
				probeInterval);
		}

		EnsureSupportProbe();
	}

	internal static void Register() =>
		ApiExtensibility.Register(typeof(IAppTaskInfoExtension), _ => Instance);

	public override bool IsSupported()
	{
		EnsureSupportProbe();
		return Volatile.Read(ref _isDisposed) == 0 && Volatile.Read(ref _supportState) == 1;
	}

	protected override async Task OnSynchronizeAsync(AppTaskInfoSnapshot[] tasks)
	{
		try
		{
			await PublishAsync(tasks);
			Volatile.Write(ref _supportState, 1);
		}
		catch (Exception error) when (IsRecoverable(error))
		{
			ResetPublicationConnection();
			Volatile.Write(ref _supportState, 0);
			Interlocked.Exchange(ref _nextSupportProbeAt, Environment.TickCount64 + _probeIntervalMilliseconds);
			throw;
		}
	}

	private async Task PublishAsync(AppTaskInfoSnapshot[] tasks)
	{
		var owner = await _service.GetOwnerAsync().ConfigureAwait(false);
		if (!string.Equals(_publishedOwner, owner, StringComparison.Ordinal))
		{
			// Notification IDs and cached payloads belong to one unique bus owner, not the reusable service name.
			_signatures.Clear();
			_notificationIds.Clear();
			_publishedOwner = owner;
		}

		var currentIds = tasks.Select(static task => task.Id).ToHashSet(StringComparer.Ordinal);
		var removedIds = _signatures.Keys.Where(id => !currentIds.Contains(id)).ToArray();

		foreach (var removedId in removedIds)
		{
			if (_notificationIds.TryGetValue(removedId, out var notificationId))
			{
				await _service.CloseAsync(owner, notificationId).ConfigureAwait(false);
				_notificationIds.Remove(removedId);
			}

			_signatures.Remove(removedId);
		}

		foreach (var task in tasks)
		{
			var payload = CreatePayload(task);
			if (_signatures.TryGetValue(task.Id, out var previous) && previous == payload.Signature)
			{
				continue;
			}

			_notificationIds.TryGetValue(task.Id, out var replacesId);
			var notificationId = await _service.NotifyAsync(
				owner,
				replacesId,
				payload.Icon,
				payload.Summary,
				payload.Body)
				.ConfigureAwait(false);
			_notificationIds[task.Id] = notificationId;
			_signatures[task.Id] = payload.Signature;
		}
	}

	private void ResetPublicationConnection()
	{
		_service.Reset();
		_publishedOwner = null;
		_signatures.Clear();
		_notificationIds.Clear();
	}

	private void EnsureSupportProbe()
	{
		if (Volatile.Read(ref _isDisposed) != 0 ||
			Environment.TickCount64 < Interlocked.Read(ref _nextSupportProbeAt))
		{
			return;
		}

		lock (_supportProbeGate)
		{
			if (_supportProbe is { IsCompleted: false })
			{
				return;
			}

			Interlocked.Exchange(ref _nextSupportProbeAt, Environment.TickCount64 + _probeIntervalMilliseconds);
			_supportProbe = ProbeSupportAsync();
			ObserveSupportProbe(_supportProbe);
		}
	}

	private static async void ObserveSupportProbe(Task probe) => await probe;

	private async Task ProbeSupportAsync()
	{
		AppTaskNotificationSupport support;
		try
		{
			support = await _service.ProbeAsync().ConfigureAwait(false);
		}
		catch (Exception error) when (IsRecoverable(error))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Unable to probe the app task notification service: {error.Message}");
			}
			support = default;
		}

		if (Volatile.Read(ref _isDisposed) != 0)
		{
			return;
		}

		var supportState = support.IsSupported ? 1 : 2;
		var previousOwner = Interlocked.Exchange(ref _supportOwner, support.Owner);
		var previousState = Interlocked.Exchange(ref _supportState, supportState);
		if (supportState == 1 &&
			(previousState != 1 || !string.Equals(previousOwner, support.Owner, StringComparison.Ordinal)))
		{
			InvalidateSynchronization();
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
		{
			_supportTimer?.Dispose();
			_service.Dispose();
		}
	}

	private static NotificationPayload CreatePayload(AppTaskInfoSnapshot task) =>
		new(GetIcon(task.IconUri), EscapeMarkup(task.Title), EscapeMarkup(GetBody(task)));

	private static string GetBody(AppTaskInfoSnapshot task)
	{
		var content = GetContentText(task);
		return string.IsNullOrEmpty(task.Content.Question)
			? $"{task.State}: {content}"
			: $"{task.Content.Question}\n{task.State}: {content}";
	}

	private static string GetContentText(AppTaskInfoSnapshot task)
	{
		if (!string.IsNullOrEmpty(task.Content.ExecutingStep))
		{
			return task.Content.ExecutingStep;
		}

		if (!string.IsNullOrEmpty(task.Content.TextSummary))
		{
			return task.Content.TextSummary;
		}

		return task.Content.GeneratedAssets.Length > 0
			? string.Join(", ", task.Content.GeneratedAssets.Select(static asset => asset.Name))
			: task.Subtitle;
	}

	private static string GetIcon(Uri iconUri) =>
		iconUri.IsFile ? iconUri.LocalPath : string.Empty;

	private static string EscapeMarkup(string value) =>
		value
			.Replace("&", "&amp;", StringComparison.Ordinal)
			.Replace("<", "&lt;", StringComparison.Ordinal)
			.Replace(">", "&gt;", StringComparison.Ordinal);

	// Deriving the change signature from the published payload keeps the two in lockstep: content that
	// the notification does not render can never be missed, and can never force a spurious re-publish.
	private readonly record struct NotificationPayload(string Icon, string Summary, string Body)
	{
		internal string Signature => string.Join('\n', Icon, Summary, Body);
	}
}
