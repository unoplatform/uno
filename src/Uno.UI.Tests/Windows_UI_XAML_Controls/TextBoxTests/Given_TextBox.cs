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
		public void When_TextChanging_Modifying_Text()
		{
			var textBox = new TextBox();
			textBox.TextChanging
				+= (o, e) =>
			  {
				  var tb = o as TextBox;
				  tb.Text = tb.Text + "Street";
			  };
			textBox.Text = "E";

			Assert.AreEqual("EStreet", textBox.Text);
		}

		[TestMethod]
		public void When_Setting_Text_Null()
		{
			var textBox = new TextBox();
			Assert.AreEqual(string.Empty, textBox.Text);
			var callbackCount = 0;
			textBox.RegisterPropertyChangedCallback(TextBox.TextProperty, (o, e) =>
			{
				callbackCount++;
			});

			textBox.Text = "Rhubarb";
			Assert.AreEqual("Rhubarb", textBox.Text);
			Assert.AreEqual(1, callbackCount);

#if NETFX_CORE
			Assert.ThrowsException<ArgumentNullException>(() => textBox.Text = null);
#else
			textBox.Text = null;
#endif
			;
			Assert.AreEqual("Rhubarb", textBox.Text);
			Assert.AreEqual(1, callbackCount);
		}

		[TestMethod]
		public void When_BeforeTextChanging_Cancel()
		{
			var textBox = new TextBox();
			textBox.Text = "Mango";
			var textChangingCount = 0;
			var beforeTextChangingCount = 0;
			textBox.BeforeTextChanging += (tb, e) =>
			  {
				  beforeTextChangingCount++;
				  if (e.NewText == "Papaya")
				  {
					  e.Cancel = true;
				  }
			  };
			textBox.TextChanging += (tb, e) => textChangingCount++;
			textBox.Text = "Chirimoya";
			Assert.AreEqual("Chirimoya", textBox.Text);
			Assert.AreEqual(1, beforeTextChangingCount);
			Assert.AreEqual(1, textChangingCount);

			textBox.Text = "Papaya";
			Assert.AreEqual("Chirimoya", textBox.Text);
			Assert.AreEqual(2, beforeTextChangingCount);
			Assert.AreEqual(1, textChangingCount);

			textBox.Text = "Chirimoya";
			Assert.AreEqual(2, beforeTextChangingCount);
		}
	}
}
