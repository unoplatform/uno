#nullable disable

#if __IOS__
using System; 
using System.Linq; 
using UIKit;

namespace Uno.UI.Helpers
{
	internal class NativeFramePresenterUIGestureRecognizerDelegate : UIGestureRecognizerDelegate
	{
		private Func<UINavigationController> _navigationController; 
		public NativeFramePresenterUIGestureRecognizerDelegate(Func<UINavigationController> navigationController)
		{
			_navigationController = navigationController;
		}
		public override bool ShouldBegin(UIGestureRecognizer recognizer)
		{
			return _navigationController()?.ViewControllers?.Length > 1;
		}
	}
}
#endif
