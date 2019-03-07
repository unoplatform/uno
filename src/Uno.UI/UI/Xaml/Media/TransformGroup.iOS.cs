using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreGraphics;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
    public partial class TransformGroup
    {
		internal override CGAffineTransform ToNativeTransform(CGSize size, bool withCenter = true)
		{
			return Children
				.Safe()
				.Select(transform => transform.ToNativeTransform(size, withCenter))
				.Aggregate(
					CGAffineTransform.MakeIdentity(),
					(transform, final) => final * transform // * is used to concatenate transformations
				);
		}
	}
}
