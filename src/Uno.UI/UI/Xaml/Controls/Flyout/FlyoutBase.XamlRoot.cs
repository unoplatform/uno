#nullable enable

using System;
using Uno.UI.DataBinding;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase
{
	private ManagedWeakReference? _visualTreeCacheWeakReference;

	/// <summary>
	/// Gets or sets the XamlRoot in which this flyout is being viewed.
	/// </summary>
	public XamlRoot? XamlRoot
	{
		get => XamlRoot.GetForElement(this);
		set => XamlRoot.SetForElement(this, XamlRoot, value);
	}

	internal VisualTree? VisualTreeCache
	{
		get => _visualTreeCacheWeakReference?.IsDisposed == false ?
			_visualTreeCacheWeakReference.Target as VisualTree : null;
		set => _visualTreeCacheWeakReference = WeakReferencePool.RentWeakReference(this, value);
	}
}
