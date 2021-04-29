#nullable enable

using System;
using System.Collections.Immutable;

namespace Windows.UI.Composition
{
	public partial class ContainerVisual : Visual
	{
		// Used by generated code only
		internal ContainerVisual() : base(null!) => throw new NotSupportedException("Use the ctor with Compositor");

		internal ContainerVisual(Compositor compositor) : base(compositor)
		{
			Children = new VisualCollection(this);
		}

		public VisualCollection Children { get; }

		/// <inheritdoc />
		private protected override void OnCommit()
		{
			base.OnCommit();

			Children.Commit();

			// Children.Commit() has only committed its internal changes,
			// it's our responsibility to propagate the Commit to our children visual.
			foreach (var child in Children.Committed)
			{
				child.Commit();
			}
		}

		/// <inheritdoc />
		private protected override void OnRenderIndependent()
		{
			base.OnRenderIndependent();

			foreach (var child in Children.Committed)
			{
				child.RenderIndependent();
			}
		}

		/// <inheritdoc />
		private protected override void OnRenderDependent()
		{
			base.OnRenderDependent();

			foreach (var child in Children.Committed)
			{
				child.RenderDependent();
			}
		}
	}
}
