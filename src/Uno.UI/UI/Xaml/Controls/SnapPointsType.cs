
using Uno;

namespace Microsoft.UI.Xaml.Controls
{
	public enum SnapPointsType
	{
		None,
#if !__SKIA__
		[NotImplemented]
#endif
		Optional,
		Mandatory,
#if !__SKIA__
		[NotImplemented]
#endif
		OptionalSingle,
		MandatorySingle,
	}
}
