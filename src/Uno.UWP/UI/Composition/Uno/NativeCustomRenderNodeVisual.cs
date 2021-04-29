#nullable enable

using System;
using System.Linq;
using System.Numerics;
using Windows.UI;
using Windows.UI.Composition;
using Android.Graphics;

namespace Uno.UI.Composition
{
	/// <summary>
	/// A <see cref="Visual"/> which allow user to create and edit it own <see cref="RenderNode"/>.
	/// </summary>
	internal sealed class NativeCustomRenderNodeVisual : Visual
	{
		private RenderNode? _uiNode;
		private RenderNode? _renderNode;

		public NativeCustomRenderNodeVisual(UIContext context)
			: base(context.Compositor)
		{
		}

		/// <summary>
		/// Sets the node that has to be used render the content
		/// </summary>
		/// <remarks>The node MUST NOT be edited once set. You have instead to create a new instance each time!</remarks>
		public RenderNode? Node
		{
			get => _uiNode;
			set
			{
				if (_uiNode == value)
				{
					return;
				}

				_uiNode = value;
				Size = value is null
					? default
					: new Vector2(value.Right, value.Bottom); // We keep Offset to 0 and use the "full size" of the sub-node so it won't be clipped!

				Invalidate(CompositionPropertyType.Dependent);
			}
		}

		/// <inheritdoc />
		private protected override void OnCommit()
		{
			base.OnCommit();

			_renderNode = _uiNode;
		}

		/// <inheritdoc />
		private protected override void RenderDependent(Canvas canvas)
		{
			base.RenderDependent(canvas);

			if (_renderNode is { } node)
			{
				canvas.DrawRenderNode(node);
			}
		}
	}
}
