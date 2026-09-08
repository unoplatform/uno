namespace Uno;

partial class WinRTFeatureConfiguration
{
	public static class AppNotifications
	{
#if __CROSSRUNTIME__
		/// <summary>
		/// Gets or sets whether WebAssembly app notifications use the persistent Service Worker API.
		/// </summary>
		/// <remarks>
		/// Set this once during application startup, before accessing <see cref="Microsoft.Windows.AppNotifications.AppNotificationManager"/>.
		/// The default is <see langword="false"/>, which uses document-scoped browser notifications.
		/// </remarks>
		public static bool UseServiceWorkerOnWebAssembly { get; set; }
#endif
	}
}