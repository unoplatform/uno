using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// Contract for DependencyObjects which react to initialization and completion of Xaml parsing.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IDependencyObjectParse
	{
		bool IsParsing { get; set; }

		void CreationComplete();
	}
}
