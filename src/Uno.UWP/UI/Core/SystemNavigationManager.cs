using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public sealed partial class SystemNavigationManager
	{
		private static SystemNavigationManager _instance;
		private AppViewBackButtonVisibility _appViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

		public AppViewBackButtonVisibility AppViewBackButtonVisibility
		{
			get => _appViewBackButtonVisibility;
			set
			{
				_appViewBackButtonVisibility = value;
				AppViewBackButtonVisibilityChanged?.Invoke(this, _appViewBackButtonVisibility);
			}
		}

		public event EventHandler<BackRequestedEventArgs> BackRequested = delegate { };

		private SystemNavigationManager()
		{

		}

		public static SystemNavigationManager GetForCurrentView()
		{
			if(_instance == null)
			{
				_instance = new SystemNavigationManager();
			}

			return _instance;
		}

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

		internal event EventHandler<AppViewBackButtonVisibility> AppViewBackButtonVisibilityChanged;
	}
}