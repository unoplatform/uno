using Android.Views;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;

namespace Windows.UI.Xaml
{
	public partial class ElementStub : FrameworkElement
	{
		public ElementStub()
		{
			Visibility = Visibility.Collapsed;
		}

		private View MaterializeContent()
		{
			var parentViewGroup = (this as View).Parent as ViewGroup;
			var currentPosition = parentViewGroup?.GetChildren().IndexOf(this);

			if (currentPosition != null && currentPosition.Value != -1)
			{
				// Create the instance first so that x:Bind constructs can be picked up by the
				// Unload event of ElementStub. Not doing so does not fills up the generated variables
				// too late and Binding.Update() does not refresh the available x:Bind instances.
				var newContent = ContentBuilder();

				parentViewGroup.RemoveViewAt(currentPosition.Value);

				var UnoViewGroup = parentViewGroup as UnoViewGroup;

				if (UnoViewGroup != null)
				{
					var newContentAsFrameworkElement = newContent as IFrameworkElement;
					if (newContentAsFrameworkElement != null)
					{
						newContentAsFrameworkElement.TemplatedParent = (UnoViewGroup as IFrameworkElement)?.TemplatedParent;
					}
					UnoViewGroup.AddView(newContent, currentPosition.Value);
				}
				else
				{
					parentViewGroup.AddView(newContent, currentPosition.Value);
				}

				return newContent;
			}

			return null;
		}
	}
}
