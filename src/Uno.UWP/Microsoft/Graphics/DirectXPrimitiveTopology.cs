#if HAS_UNO_WINUI
using Microsoft.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.Graphics.DirectX
{
	[ContractVersion(typeof(LiftedContract), 65536u)]
	public enum DirectXPrimitiveTopology
	{
		Undefined,
		PointList,
		LineList,
		LineStrip,
		TriangleList,
		TriangleStrip
	}
}
#endif
