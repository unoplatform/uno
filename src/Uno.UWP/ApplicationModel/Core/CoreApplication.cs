#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System.Collections.Generic;
using Uno.Helpers.Theming;

namespace Windows.ApplicationModel.Core
{
	public  partial class CoreApplication 
	{
		private static CoreApplicationView _currentView;
		private static List<CoreApplicationView> _views;

		public static event global::System.EventHandler<object> Resuming;

		public static event global::System.EventHandler<global::Windows.ApplicationModel.SuspendingEventArgs> Suspending;

		static CoreApplication()
		{
			_currentView = new CoreApplicationView();
		}

		/// <summary>
		/// Raises the <see cref="Resuming"/> event
		/// </summary>
		internal static void RaiseResuming()
			=> Resuming?.Invoke(null, null);

		/// <summary>
		/// Raises the <see cref="Suspending"/> event
		/// </summary>
		internal static void RaiseSuspending(SuspendingEventArgs args)
			=> Suspending?.Invoke(null, args);

		public static CoreApplicationView GetCurrentView()
			=> _currentView;

		public static global::Windows.ApplicationModel.Core.CoreApplicationView MainView
			=> _currentView;

		public static IReadOnlyList<CoreApplicationView> Views
		{
			get
			{
				if(_views == null)
				{
					_views = new List<CoreApplicationView> { _currentView };
				}

				return _views;
			}
		}

		/// <summary>
		/// This property is kept in sync with the Application.RequestedTheme to enable
		/// native UI elements in non Uno.UWP to resolve the currently set Application theme.
		/// </summary>
		internal static SystemTheme RequestedTheme { get; set; }
	}
}
