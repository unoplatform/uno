#nullable enable

using System.Numerics;
using System;

namespace Windows.UI.Composition
{
	public partial class Visual : global::Windows.UI.Composition.CompositionObject
	{
		internal object? NativeOwner { get; set; }
	}
}
