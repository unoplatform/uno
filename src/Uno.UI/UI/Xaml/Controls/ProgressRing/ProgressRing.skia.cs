using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

using Microsoft/*Intentional space for WinUI upgrade tooling*/.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls
{
	public partial class ProgressRing : Control
	{
		private static void OnIsActiveChanged(DependencyObject dependencyObject, object newValue) => throw new NotSupportedException();
	}
}
