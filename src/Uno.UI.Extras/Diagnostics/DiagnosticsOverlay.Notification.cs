#nullable enable
#if WINUI || HAS_UNO_WINUI
using System.Linq;
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

#pragma warning disable IDE0051 // Not used on windows UWP
	private void Notify(DiagnosticViewNotification notif, ViewContext context)
	{
		if (!_isVisible
			|| (_isExpanded && !notif.Options.HasFlag(DiagnosticViewNotificationDisplayOptions.EvenIfExpended)))
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
			presenter.ContentTemplate = notif.ContentTemplate as DataTemplate;
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

		// Sever the presenter's Content/ContentTemplate: a notification raised by a diagnostic view
		// can carry a DataTemplate (or content object) defined in that view's collectible
		// AssemblyLoadContext. Collapsing the visual state alone leaves those references on the
		// process-lifetime presenter, pinning the ALC (its generated resources → LoaderAllocator)
		// after the view is gone (#23614). The presenter re-populates on the next Notify.
		if (_notificationPresenter is { } presenter)
		{
			presenter.Content = null;
			presenter.ContentTemplate = null;
		}
	}
}
#endif
