using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.ContentDialogTests
{
	[TestClass]
	public class Given_ContentDialog
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_Has_DataContext()
		{
			var dc = "Starfish";
			var border = new Border();
			var dialog = new ContentDialog
			{
				DataContext = dc,
				Content = border
			};

			dialog.ForceLoaded();

			Assert.AreEqual("Starfish", dialog.DataContext);
			var dummy = dialog.ShowAsync();
			Assert.AreEqual("Starfish", border.DataContext);
		}
	}
}
