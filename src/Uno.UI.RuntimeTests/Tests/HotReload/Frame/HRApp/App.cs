using Uno.UI;

#if HAS_UNO_WINUI
using LaunchActivatedEventArgs = Microsoft/* UWP don't rename */.UI.Xaml.LaunchActivatedEventArgs;
#else
using LaunchActivatedEventArgs = Windows.ApplicationModel.Activation.LaunchActivatedEventArgs;
#endif

namespace UnoApp50
{
	public class App : Application
	{
		public static Window? _window;

		protected internal override void OnLaunched(LaunchActivatedEventArgs args)
		{
#if NET6_0_OR_GREATER && WINDOWS && !HAS_UNO
			_window = new Window();
#else
			_window = Microsoft.UI.Xaml.Window.Current;
#endif
			_window.EnableHotReload();
			// Do not repeat app initialization when the Window already has content,
			// just ensure that the window is active
			if (_window!.Content is not Frame rootFrame)
			{
				// Create a Frame to act as the navigation context and navigate to the first page
				rootFrame = new Frame();

				// Place the frame in the current Window
				_window.Content = rootFrame;

				rootFrame.NavigationFailed += OnNavigationFailed;
			}

			if (rootFrame.Content == null)
			{
				// When the navigation stack isn't restored navigate to the first page,
				// configuring the new page by passing required information as a navigation
				// parameter
				rootFrame.Navigate(typeof(MainPage), args.Arguments);
			}

			// Ensure the current window is active
			_window!.Activate();
		}

		/// <summary>
		/// Invoked when Navigation to a certain page fails
		/// </summary>
		/// <param name="sender">The Frame which failed navigation</param>
		/// <param name="e">Details about the navigation failure</param>
		void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			throw new InvalidOperationException($"Failed to load {e.SourcePageType.FullName}: {e.Exception}");
		}
	}
}
