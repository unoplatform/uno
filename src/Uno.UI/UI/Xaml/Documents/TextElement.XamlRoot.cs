#nullable enable

using System;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Documents;

public partial class TextElement
{
	/// <summary>
	/// Gets or sets the XamlRoot in which this element is being viewed.
	/// </summary>
	public
#if __WASM__
	new
#endif
	XamlRoot? XamlRoot
	{
		get => XamlRoot.GetForElement(this);
		set => XamlRoot.SetForElement(this, XamlRoot, value);
	}
}
