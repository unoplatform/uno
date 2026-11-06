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
		set
		{
			var xamlRoot = XamlRoot.GetForElement(this);
			if (value == xamlRoot)
			{
				return;
			}

			if (xamlRoot is not null)
			{
				throw new InvalidOperationException("Cannot change XamlRoot when it is not null.");
			}

			XamlRoot.SetForElement(this, xamlRoot, value);
		}
	}
}
