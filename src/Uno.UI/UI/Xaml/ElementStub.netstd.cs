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
        public ElementStub()
        {
            Visibility = Visibility.Collapsed;
        }

        private FrameworkElement MaterializeContent()
        {
            if(Parent is FrameworkElement parentElement)
            {
                var currentPosition = parentElement.GetChildren().IndexOf(this);

                if (currentPosition != -1)
                {
					// Create the instance first so that x:Bind constructs can be picked up by the
					// Unload event of ElementStub. Not doing so does not fills up the generated variables
					// too late and Binding.Update() does not refresh the available x:Bind instances.
					var newContent = ContentBuilder() as UIElement;

                    parentElement.RemoveChild(this);

                    parentElement.AddChild(newContent, currentPosition);

					return newContent as FrameworkElement;
                }
            }

            return null;
        }
    }
}
