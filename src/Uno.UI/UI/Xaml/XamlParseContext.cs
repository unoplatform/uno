using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml
{
	/// <summary>
	/// Provides additional information on the context in which Xaml is being parsed by Uno.
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class XamlParseContext
	{
		// *************** WARNING ***************
		// This class instance is not being replaced when a ResourceDictionary is being reloaded (i.e. we continue to use the instance of the original type)
		// This is valid only because all information hosted by this class doesn't change on hot-reload.
		// 
		// Any new property on this class should follow this same rule, or the resolution of this has to be changed to support hot-reload properly
		// (search for "__ParseContext_" in the xaml generator).
		// ***************************************

		public string AssemblyName { get; set; }
	}
}
