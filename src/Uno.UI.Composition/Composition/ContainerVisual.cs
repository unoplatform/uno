#nullable enable

using System;

namespace Microsoft.UI.Composition
{
	public partial class ContainerVisual : Visual
	{
		internal ContainerVisual() : base(null!) => throw new NotSupportedException("Use the ctor with Compositor");

		internal ContainerVisual(Compositor compositor) : base(compositor)
		{
			Children = new VisualCollection(compositor, this);
		}

		public VisualCollection Children { get; }
	}
}
