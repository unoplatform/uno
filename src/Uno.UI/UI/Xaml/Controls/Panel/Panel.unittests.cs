using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System.Drawing;
using View = Microsoft.UI.Xaml.UIElement;


namespace Windows.UI.Xaml.Controls;

public partial class Panel : FrameworkElement
{
	protected virtual void OnChildrenChanged()
	{
	}

	public override IEnumerable<View> GetChildren()
		=> Children.OfType<View>().ToArray<View>();

	bool ICustomClippingElement.AllowClippingToLayoutSlot => false;
	bool ICustomClippingElement.ForceClippingToLayoutSlot => CornerRadiusInternal != CornerRadius.None;
}
