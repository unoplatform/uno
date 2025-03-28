using System;
using System.Linq;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
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
