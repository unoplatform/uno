#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Uno.Diagnostics.UI;

public sealed partial class DiagnosticsOverlay
{
	private CancellationTokenSource? _notification;

	private void OnNotificationTapped(object sender, TappedRoutedEventArgs e)
	{
		HideNotification();
	}

	private void Notify(DiagnosticViewNotification notif, Context context)
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

	private void HideNotification()
	{
		VisualStateManager.GoToState(this, NotificationCollapsedStateName, true);
	}
}
