using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		#region CanDrag (DP)
		public static DependencyProperty CanDragProperty { get; } = DependencyProperty.Register(
			nameof(CanDrag),
			typeof(bool),
			typeof(UIElement),
			new FrameworkPropertyMetadata(default(bool)));

		public bool CanDrag
		{
			get => (bool)GetValue(AllowDropProperty);
			set => SetValue(AllowDropProperty, value);
		} 
		#endregion

		#region AllowDrop (DP)
		public static DependencyProperty AllowDropProperty { get; } = DependencyProperty.Register(
			nameof(AllowDrop),
			typeof(bool),
			typeof(UIElement),
			new FrameworkPropertyMetadata(default(bool)));

		public bool AllowDrop
		{
			get => (bool)GetValue(CanDragProperty);
			set => SetValue(CanDragProperty, value);
		} 
		#endregion
	}
}
