#if !UNO_REFERENCE_API

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using Uno;

namespace Windows.UI.Xaml.Controls;

partial class TimePicker
{
	#region FlyoutPlacement DependencyProperty

	[UnoOnly]
	public FlyoutPlacementMode FlyoutPlacement
	{
		get => (FlyoutPlacementMode)this.GetValue(FlyoutPlacementProperty);
		set => this.SetValue(FlyoutPlacementProperty, value);
	}

	[UnoOnly]
	public static DependencyProperty FlyoutPlacementProperty { get; } =
		DependencyProperty.Register(
			nameof(FlyoutPlacement),
			typeof(FlyoutPlacementMode),
			typeof(TimePicker),
			new FrameworkPropertyMetadata(FlyoutPlacementMode.Full));

	#endregion
}

#endif
