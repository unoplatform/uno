using System.Windows.Input;
using Windows.UI.Core;
using Uno.UI.Samples.UITests.Helpers;
using Private.Infrastructure;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	internal class WebView2StaticViewModel : ViewModelBase
	{
		private static readonly string[] WebSites =
		{
			"http://microsoft.com",
			"http://nventive.com",
			"http://xamarin.com",
			"http://burymewithmymoney.com/"
		};

		private string _webSource;
		private int _nextLink = 0;

		public WebView2StaticViewModel(UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		private void GoNextLink()
		{
			_nextLink++;
			var link = WebSites[_nextLink % WebSites.Length];
			WebSource = link;
		}

		public string ChromeTestUri => "https://presgit.github.io/camera.html";

		public string WebSource
		{
			get => _webSource;
			private set
			{
				_webSource = value;
				RaisePropertyChanged();
			}
		}

		public ICommand GoToNextWebSource => GetOrCreateCommand(GoNextLink);
	}
}
