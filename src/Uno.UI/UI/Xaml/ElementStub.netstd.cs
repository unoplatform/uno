using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;

namespace Windows.UI.Xaml
{
	public partial class ElementStub
	{
		private FrameworkElement SwapViews(FrameworkElement oldView, Func<FrameworkElement> newViewProvider)
		{
			if (oldView?.Parent is FrameworkElement parentElement)
			{
				var currentPosition = parentElement.GetChildren().IndexOf(oldView);

				if (currentPosition != -1)
				{
					var newView = newViewProvider();

					parentElement.RemoveChild(oldView);
					parentElement.AddChild(newView, currentPosition);

					return newView;
				}
			}

			return null;
		}
	}
}
