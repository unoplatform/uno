using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.TextBoxTests
{
	[TestClass]
	public class Given_TextBox
	{
		[TestMethod]
		public void When_TextChanged_Modifying_Text()
		{
			var textBox = new TextBox();
#if NETFX_CORE // On UWP TextChanged isn't fired until the UI has updated, but TextChanging isn't currently raised in Uno
			textBox.TextChanging
#else
			textBox.TextChanged
#endif
				+= (o, e) =>
			  {
				  var tb = o as TextBox;
				  tb.Text = tb.Text + "Street";
			  };
			textBox.Text = "E";

			Assert.AreEqual("EStreet", textBox.Text);
		}
	}
}
