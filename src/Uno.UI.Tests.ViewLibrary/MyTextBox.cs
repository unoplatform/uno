using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.ViewLibrary
{
	public partial class MyTextBox : TextBox
	{
		public MyTextBox()
		{
			DefaultStyleKey = typeof(MyTextBox);
		}
	}
}
