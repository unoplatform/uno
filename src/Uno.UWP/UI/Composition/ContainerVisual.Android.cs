using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Android.Graphics;

namespace Windows.UI.Composition
{
	partial class ContainerVisual
	{
		//private ImmutableList<Visual> _renderChildren = ImmutableList<Visual>.Empty;

		///// <inheritdoc />
		//internal override void Commit()
		//{
		//	base.Commit();

		//	_renderChildren = Children.ToImmutable();
		//	foreach (var child in _renderChildren)
		//	{
		//		child.Commit();
		//	}
		//}

		///// <inheritdoc />
		//private protected override void RenderIndependent(RenderNode node)
		//{
		//	base.RenderIndependent(node);


		//}

		///// <inheritdoc />
		//private protected override void RenderDependent(Canvas canvas)
		//{
		//	base.RenderDependent(canvas);

		//	foreach (var child in _renderChildren)
		//	{
		//		child.DrawOn(canvas);
		//	}
		//}
	}
}
