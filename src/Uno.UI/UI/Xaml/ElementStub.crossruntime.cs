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
		private FrameworkElement SwapViews(FrameworkElement oldView, Func<object> newViewProvider)
		{
			if (oldView?.Parent is FrameworkElement parentElement)
			{
				var currentPosition = parentElement.GetChildren().IndexOf(oldView);

				if (currentPosition != -1)
				{
					var newView = (FrameworkElement)newViewProvider();

					parentElement.RemoveChild(oldView);

					RaiseMaterializing();

					parentElement.AddChild(newView, currentPosition);

					return newView;
				}
			}

			return null;
		}
	}
}
