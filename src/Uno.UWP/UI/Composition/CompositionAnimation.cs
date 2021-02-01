#nullable enable

using System;
using Uno;

namespace Windows.UI.Composition
{
	public  partial class CompositionAnimation
	{
		internal CompositionAnimation() => throw new NotSupportedException("Use the ctor with Compositor");

		internal CompositionAnimation(Compositor compositor) : base(compositor)
		{
		}
	}
}
