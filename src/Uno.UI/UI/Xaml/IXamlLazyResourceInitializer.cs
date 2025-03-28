using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Resources;

namespace Uno.UI
{
	/// <summary>
	/// Provides initialization logic for resources defined in a ResourceDictionary.
	/// </summary>
	/// <remarks> Normally only implemented and referenced by Xaml-generated code.</remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public interface IXamlLazyResourceInitializer
	{
		object GetInitializedValue(string resourceRetrievalKey);
	}
}
