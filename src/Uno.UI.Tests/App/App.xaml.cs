using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UnitTestsApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class App : Application
	{
		public Grid HostView { get; private set; }
		public App()
		{
			this.InitializeComponent();
		}

		protected
#if !NETFX_CORE
			internal
#endif
			override void OnLaunched(LaunchActivatedEventArgs args)
		{
			if (HostView == null)
			{
				HostView = new Grid() { Name = "HostView" };

				Window.Current.Content = HostView;

				Window.Current.Activate();
			}

			OnLaunchedPartial();
		}

		partial void OnLaunchedPartial();

		/// <summary>
		/// Ensure that application exists, for unit tests. 
		/// </summary>
		/// <returns>The 'running' application.</returns>
		public static App EnsureApplication()
		{
			if (Current == null)
			{
				var application = new App();
				application.InitializationCompleted();
				application.OnLaunched(null);
			}

			var app = Current as App;			
			app.HostView.Children.Clear();

			return app;
		}
	}
}
