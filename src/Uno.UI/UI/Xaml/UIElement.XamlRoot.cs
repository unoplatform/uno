#nullable enable

using System;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

public partial class UIElement : DependencyObject, IXUidProvider
{
	public XamlRoot? XamlRoot
	{
		get => XamlRoot.GetForElement(this);
		set => XamlRoot.SetForElement(this, XamlRoot, value);
	}
}
