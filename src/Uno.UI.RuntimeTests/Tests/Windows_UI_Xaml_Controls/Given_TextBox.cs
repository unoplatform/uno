using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_TextBox
	{
#if __ANDROID__
		[TestMethod]
		public void When_InputScope_Null_And_ImeOptions()
		{
			var tb = new TextBox();
			tb.InputScope = null;
			tb.ImeOptions = Android.Views.InputMethods.ImeAction.Search;
		}
#endif
	}
}
