#if __APPLE_UIKIT__
using UIKit;

namespace Windows.UI.ViewManagement;

public partial class UISettings
{
	public double TextScaleFactor
	{
		get => UIFont.PreferredBody.PointSize / 17.0;
	}
}
#endif
