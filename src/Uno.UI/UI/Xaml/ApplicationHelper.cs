using Windows.UI.Xaml;

namespace Uno.UI
{
	public class ApplicationHelper
	{
		private static string _requestedCustomTheme;

		public static string RequestedCustomTheme
		{
			get => _requestedCustomTheme;
			set
			{
				_requestedCustomTheme = value;

				if (_requestedCustomTheme.Equals("Dark"))
				{
					Application.Current.RequestedTheme = ApplicationTheme.Dark;
				}
				else if (_requestedCustomTheme.Equals("Light"))
				{
					Application.Current.RequestedTheme = ApplicationTheme.Light;
				}
			}
		}
	}
}
