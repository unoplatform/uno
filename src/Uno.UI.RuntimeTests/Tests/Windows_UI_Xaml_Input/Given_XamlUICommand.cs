using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Input;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input;

[TestClass]
public class Given_XamlUICommand
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_KeyboardAccelerators_Retrieved()
	{
		var xamlUICommand = new XamlUICommand();
		var keyboardAccelerators = xamlUICommand.KeyboardAccelerators;
		Assert.IsNotNull(keyboardAccelerators);
	}
}
