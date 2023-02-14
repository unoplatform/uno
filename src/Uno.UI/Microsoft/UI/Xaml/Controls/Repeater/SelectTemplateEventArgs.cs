using System;
using System.Linq;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public sealed partial class SelectTemplateEventArgs
	{
		internal SelectTemplateEventArgs()
		{
		}

		public string TemplateKey { get; set; }
		public object DataContext { get; internal set; }
		public UIElement Owner { get; internal set; }
	}
}
