#nullable enable
#if WINUI || HAS_UNO_WINUI
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace Uno.Diagnostics.UI;

public sealed partial class DiagnosticsOverlay
{
	private CancellationTokenSource? _notification;

	private void OnNotificationTapped(object sender, TappedRoutedEventArgs e)
	{
		HideNotification();
	}

#pragma warning disable IDE0051 // Not used on windows UWP
	private void Notify(DiagnosticViewNotification notif, ViewContext context)
	{
		if (!_isVisible || _isExpanded)
		{
			return;
		}

		var ct = new CancellationTokenSource();
		if (Interlocked.Exchange(ref _notification, ct) is { IsCancellationRequested: false } previous)
		{
			previous.Cancel();
		}

		if (_notification is not null)
		{
			context.Schedule(() => _ = ShowNotification());
		}

		async ValueTask ShowNotification()
		{
			var presenter = _notificationPresenter;
			if (presenter is null)
			{
				return;
			}

			presenter.Content = notif.Content;
			VisualStateManager.GoToState(this, NotificationVisibleStateName, true);

			if (notif.Duration is { TotalMilliseconds: > 0 } duration)
			{
				await Task.Delay(duration, ct.Token);
				HideNotification();
			}
		}
	}
#pragma warning restore IDE0051

	private void HideNotification()
	{
		VisualStateManager.GoToState(this, NotificationCollapsedStateName, true);
	}
}
#endif
