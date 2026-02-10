using System;

namespace Windows.UI.Core
{
	public sealed partial class SystemNavigationManager
	{
		internal static SystemNavigationManager Instance { get; } = new();

		// If Android 16+ (SDK 36+) we must always handle back presses
		// when any subscriber is present.
		private readonly bool _backIsAlwaysHandled =
			OperatingSystem.IsAndroid() && OperatingSystem.IsAndroidVersionAtLeast(36);

		public static SystemNavigationManager GetForCurrentView() => Instance;

		internal event EventHandler<BackRequestedEventArgs> InternalBackRequested = delegate { };

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
						OnBackHandlerStateChanged();
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
						OnBackHandlerStateChanged();
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

		/// <summary>
		/// Gets whether there are any internal back button listeners
		/// (e.g. via <see cref="DirectUI.BackButtonIntegration"/>).
		/// </summary>
		internal bool HasInternalBackListeners { get; private set; }

		/// <summary>
		/// Gets whether there are any back handlers, either public <see cref="BackRequested"/>
		/// subscribers or internal listeners.
		/// </summary>
		internal bool HasAnyBackHandlers => HasBackRequestedSubscribers || HasInternalBackListeners;

		internal void SetHasInternalBackListeners(bool hasListeners)
		{
			if (HasInternalBackListeners != hasListeners)
			{
				HasInternalBackListeners = hasListeners;
				OnBackHandlerStateChanged();
			}
		}

		partial void OnBackHandlerStateChanged();

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
			InternalBackRequested?.Invoke(this, args);

			if (!args.Handled)
			{
				var handlers = _backRequested;
				handlers?.Invoke(this, args);
			}

			return _backIsAlwaysHandled && HasAnyBackHandlers ? true : args.Handled;
		}
	}
}
