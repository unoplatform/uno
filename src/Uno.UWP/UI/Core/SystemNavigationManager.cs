using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public sealed partial class SystemNavigationManager
	{
		private static SystemNavigationManager _instance;

		public static SystemNavigationManager GetForCurrentView()
		{
			if (_instance == null)
			{
				_instance = new SystemNavigationManager();
			}

			return _instance;
		}

		private readonly object _backRequestedLock = new object();
		private EventHandler<BackRequestedEventArgs> _backRequested;

		/// <summary>
		/// Occurs when the user presses the hardware back button (or equivalent gesture).
		/// </summary>
		/// <remarks>
		/// On Android 16+, the subscription state determines whether the app handles back navigation.
		/// When subscribed, back presses are consumed by the app. When unsubscribed, the system handles back navigation.
		/// The <see cref="BackRequestedEventArgs.Handled"/> property is ignored on Android 16+.
		/// </remarks>
		public event EventHandler<BackRequestedEventArgs> BackRequested
		{
			add
			{
				lock (_backRequestedLock)
				{
					var isFirstSubscriber = _backRequested is null;
					_backRequested += value;
					if (isFirstSubscriber)
					{
						OnBackRequestedSubscribersChanged(hasSubscribers: true);
					}
				}
			}
			remove
			{
				lock (_backRequestedLock)
				{
					_backRequested -= value;
					if (_backRequested is null)
					{
						OnBackRequestedSubscribersChanged(hasSubscribers: false);
					}
				}
			}
		}

		/// <summary>
		/// Gets whether there are any subscribers to the <see cref="BackRequested"/> event.
		/// </summary>
		internal bool HasBackRequestedSubscribers
		{
			get
			{
				lock (_backRequestedLock)
				{
					return _backRequested is not null;
				}
			}
		}

		partial void OnBackRequestedSubscribersChanged(bool hasSubscribers);

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
		/// <returns>True if the BackRequested event was handled.</returns>
		internal bool RequestBack()
		{
			var args = new BackRequestedEventArgs();
			_backRequested?.Invoke(this, args);

			return args.Handled;
		}
	}
}
