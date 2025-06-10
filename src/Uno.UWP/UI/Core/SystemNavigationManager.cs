using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public sealed partial class SystemNavigationManager
	{
		internal static SystemNavigationManager Instance { get; } = new();

		public static SystemNavigationManager GetForCurrentView() => Instance;

		public event EventHandler<BackRequestedEventArgs> BackRequested = delegate { };

		private AppViewBackButtonVisibility _appViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

		private SystemNavigationManager()
		{
		}

		public AppViewBackButtonVisibility AppViewBackButtonVisibility
		{
			get => _appViewBackButtonVisibility;
			set
			{
				if (_appViewBackButtonVisibility == value)
				{
					return;
				}

				_appViewBackButtonVisibility = value;
				AppViewBackButtonVisibilityChanged?.Invoke(this, _appViewBackButtonVisibility);
				OnAppViewBackButtonVisibility(value);
			}
		}

		internal event EventHandler<AppViewBackButtonVisibility> AppViewBackButtonVisibilityChanged;
		partial void OnAppViewBackButtonVisibility(AppViewBackButtonVisibility visibility);

		/// <summary>
		/// Raise BackRequested
		/// </summary>
		/// <returns>True is the BackRequested event was handled.</returns>
		internal bool RequestBack()
		{
			var args = new BackRequestedEventArgs();
			BackRequested?.Invoke(this, args);

			return args.Handled;
		}
	}
}
