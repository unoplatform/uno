using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	public partial class TransformGroup
	{
		internal override Matrix3x2 ToNativeTransform(Size size)
		{
			return Children
				.Safe()
				.Select(transform => transform.ToNativeTransform(size))
				.Aggregate(
					Matrix3x2.Identity,
					(transform, final) => final * transform // * is used to concatenate transformations
				);
		}
	}
}
