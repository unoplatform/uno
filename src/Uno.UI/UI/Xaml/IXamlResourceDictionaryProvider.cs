using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Resources;

namespace Uno.UI
{
	/// <summary>
	/// Provides lazy initialization for a resource dictionary.
	/// </summary>
	/// <remarks> Normally only implemented and referenced by Xaml-generated code.</remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IXamlResourceDictionaryProvider
	{
		ResourceDictionary GetResourceDictionary();
	}
}
