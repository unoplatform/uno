using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public sealed partial class SystemNavigationManager
	{
		private static SystemNavigationManager _instance;

		private event EventHandler<BackRequestedEventArgs> _backRequested;
		private bool _hasBackRequestedSubscribers;

		public static SystemNavigationManager GetForCurrentView()
		{
			if (_instance == null)
			{
				_instance = new SystemNavigationManager();
			}

			return _instance;
		}

		public event EventHandler<BackRequestedEventArgs> BackRequested
		{
			add
			{
				_backRequested += value;
				var hadSubscribers = _hasBackRequestedSubscribers;
				_hasBackRequestedSubscribers = true;
				if (!hadSubscribers)
				{
					BackHandlerRequired?.Invoke(this, true);
				}
			}
			remove
			{
				_backRequested -= value;
				var hadSubscribers = _hasBackRequestedSubscribers;
				_hasBackRequestedSubscribers = _backRequested != null;
				if (hadSubscribers && !_hasBackRequestedSubscribers)
				{
					BackHandlerRequired?.Invoke(this, false);
				}
			}
		}

		/// <summary>
		/// Event raised when the back handler requirement changes.
		/// Used by platform-specific code to enable/disable native back button handling.
		/// </summary>
		internal event EventHandler<bool> BackHandlerRequired;

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
			_backRequested?.Invoke(this, args);

			return args.Handled;
		}

		/// <summary>
		/// Gets whether there are any subscribers to the BackRequested event.
		/// </summary>
		internal bool HasBackRequestedSubscribers => _hasBackRequestedSubscribers;
	}
}
