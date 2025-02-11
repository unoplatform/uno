using _View = UIKit.UIView;

namespace Uno.Extensions
{
	public static class UIViewExtensions
	{
		public static _View FindFirstResponder(this _View view)
		{
			if (view.IsFirstResponder)
			{
				return view;
			}
			foreach (_View subView in view.Subviews)
			{
				var firstResponder = subView.FindFirstResponder();
				if (firstResponder != null)
				{
					return firstResponder;
				}
			}
			return null;
		}
	}
}
