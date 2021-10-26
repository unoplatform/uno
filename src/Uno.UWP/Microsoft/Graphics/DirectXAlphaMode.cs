#if HAS_UNO_WINUI
using System;
using Microsoft.Foundation;
using Windows.Foundation.Metadata;

namespace Microsoft.Graphics.DirectX
{
	[ContractVersion(typeof(LiftedContract), 65536u)]
	public enum DirectXAlphaMode
	{
		Unspecified,
		Premultiplied,
		Straight,
		Ignore
	}
}
#endif
