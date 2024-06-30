using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace __Uno.UI.Xaml.Controls;
internal partial class TextBlock
{
	internal static partial class NativeMethods
	{
		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.TextBlock.select")]
		internal static partial bool Select(IntPtr htmlId, int start, int length);

		[JSImport("globalThis.Microsoft.UI.Xaml.Controls.TextBlock.getSelectedText")]
		internal static partial string GetSelectedText(IntPtr htmlId);
	}
}
