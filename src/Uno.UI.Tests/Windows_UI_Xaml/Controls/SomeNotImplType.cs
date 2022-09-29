#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Tests.Windows_UI_Xaml.Controls
{
	[Uno.NotImplemented]
	public class SomeNotImplType
	{
		public static int CreationAttempts { get; private set; }
		[Uno.NotImplemented]
		public SomeNotImplType()
		{
			CreationAttempts++;
			throw new NotImplementedException();
		}
		public double SomeProperty { get; set; }
	}
}
