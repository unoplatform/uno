using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// Internal interface shared between <see cref="ContentControl"/> and <see cref="ContentPresenter"/>
	/// </summary>
	internal interface IContentHost
	{
		object Content { get; set; }

		/// <summary>
		/// Is this a generated container whose <see cref="Content"/> should be data-bound to its DataContext? Used by <see cref="ItemsControl"/>.
		/// </summary>
		bool IsGeneratedContainerNeedingItemBind { get; set; }
	}
}
