#nullable enable

using System;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase
{
	/// <summary>
	/// Gets or sets the XamlRoot in which this flyout is being viewed.
	/// </summary>
	public XamlRoot? XamlRoot
	{
		get => XamlRoot.GetForElement(this);
		set => XamlRoot.SetForElement(this, XamlRoot, value);
	}
}
