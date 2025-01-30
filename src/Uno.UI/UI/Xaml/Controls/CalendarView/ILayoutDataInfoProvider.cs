using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls
{
	internal interface ILayoutDataInfoProvider
	{
		int GetTotalItemCount();

		int GetTotalGroupCount();
	}
}
