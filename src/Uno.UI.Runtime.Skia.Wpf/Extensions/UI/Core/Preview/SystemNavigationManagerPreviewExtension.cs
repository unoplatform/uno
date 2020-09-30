using System.Windows;
using Uno.UI.Core.Preview;

namespace Uno.Extensions.UI.Core.Preview
{
	internal class SystemNavigationManagerPreviewExtension : ISystemNavigationManagerPreviewExtension
	{
		public void RequestNativeAppClose() => Application.Current.MainWindow.Close();
	}
}
