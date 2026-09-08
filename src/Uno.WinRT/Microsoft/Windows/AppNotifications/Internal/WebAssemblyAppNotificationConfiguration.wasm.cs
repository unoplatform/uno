#nullable enable

using Uno;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class WebAssemblyAppNotificationConfiguration
{
	private static readonly object _gate = new();
	private static bool _isCaptured;
	private static bool _useServiceWorker;

	public static bool UseServiceWorker
	{
		get
		{
			lock (_gate)
			{
				if (!_isCaptured)
				{
					_useServiceWorker = WinRTFeatureConfiguration.AppNotifications.UseServiceWorkerOnWebAssembly;
					_isCaptured = true;
				}
				return _useServiceWorker;
			}
		}
	}

	public static void Capture() => _ = UseServiceWorker;
}