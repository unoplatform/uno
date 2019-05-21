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
                    parentElement.RemoveChild(this);

                    var newContent = ContentBuilder() as UIElement;

                    parentElement.AddChild(newContent, currentPosition);
                }
            }

            return null;
        }
    }
}
