using System;
using System.Linq;

namespace Windows.UI.Xaml.Controls
{
	internal interface ILayoutDataInfoProvider
	{
		int GetTotalItemCount();

		int GetTotalGroupCount();
	}
}
