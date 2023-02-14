using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace XamlGenerationTests.Core
{
	public partial class NativeObject : Panel
	{
	}

	public class TestPanel
	{
		public TestPanel()
		{
			new NativeObject();
		}
	}
}
