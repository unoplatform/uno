using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Data;

/// <summary>
/// Provides a data source that adds grouping and current-item support to collection classes.
/// </summary>
public partial class CollectionViewSource : DependencyObject
{
	/// <summary>
	/// Initializes a new instance of the CollectionViewSource class.
	/// </summary>
	public CollectionViewSource()
	{
		InitializeBinder();
	}
}
