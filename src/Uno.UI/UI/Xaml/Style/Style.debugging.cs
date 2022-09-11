#if DEBUG
#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml
{
	public partial class Style
	{
		/// <summary>
		/// Debugging aid which returns the resource key associated with this resource, if it came from a <see cref="ResourceDictionary"/>.
		/// </summary>
		/// <remarks>Note: The DEBUG_SET_RESOURCE_SOURCE symbol must be set in <see cref="ResourceDictionary"/> for this to return a value.</remarks>
		public string ResourceNameDebug => this.GetResourceNameDebug();

		public ResourceDictionary? ContainingResourceDictionaryDebug => this.GetContainingResourceDictionaryDebug();
	}
}
#endif
