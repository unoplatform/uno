#nullable disable

using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// Defines an instance that can provide a <see cref="WeakReference"/> of itself.
	/// </summary>
    public interface IWeakReferenceProvider
    {
		/// <summary>
		/// A managed weak reference of the current instance.
		/// </summary>
		ManagedWeakReference WeakReference { get; }
    }
}
