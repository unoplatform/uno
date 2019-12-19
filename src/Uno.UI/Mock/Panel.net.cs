using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using System.Drawing;
using View = Windows.UI.Xaml.UIElement;


namespace Windows.UI.Xaml.Controls
{
    public partial class Panel : FrameworkElement
    {

		public Panel()
        {
            Initialize();
        }

        partial void Initialize();
        private void OnChildrenChanged()
        {
            throw new NotImplementedException();
        }

		public override IEnumerable<View> GetChildren()
			=> Children.OfType<View>().ToArray<View>();

		bool ICustomClippingElement.AllowClippingToLayoutSlot => false;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadius != CornerRadius.None;
	}
}
