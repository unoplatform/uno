using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Uno.UI.DataBinding;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface ITemplatedParentProvider
{
	ManagedWeakReference GetTemplatedParentWeakRef();

	DependencyObject GetTemplatedParent();

	void SetTemplatedParent(DependencyObject parent);
}

[EditorBrowsable(EditorBrowsableState.Never)]
/// <summary>
/// Marker interface used to block DependencyObjectGenerator
/// from injecting <see cref="ITemplatedParentProvider"/> and its implementations.
/// </summary>
public interface INotTemplatedParentProvider
{
}
