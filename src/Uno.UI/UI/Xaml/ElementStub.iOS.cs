using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Windows.UI.Xaml
{
	public partial class ElementStub : FrameworkElement
	{
		public ElementStub()
		{
			Visibility = Visibility.Collapsed;
		}

		private UIView MaterializeContent()
		{
			var currentPosition = Superview?.Subviews.IndexOf(this) ?? -1;
			
			if (currentPosition != -1)
			{
				var currentSuperview = Superview;
				RemoveFromSuperview();

				var newContent = ContentBuilder();

				currentSuperview?.InsertSubview(newContent, currentPosition);
				
				return newContent;				
			}

			return null;
		}
	}
}
