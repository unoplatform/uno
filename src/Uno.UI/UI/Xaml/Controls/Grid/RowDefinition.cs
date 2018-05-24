using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class RowDefinition : DependencyObject
	{
		public RowDefinition()
		{
			InitializeBinder();
			IsAutoPropertyInheritanceEnabled = false;
		}

		#region Height DependencyProperty

		public GridLength Height
		{
			get { return (GridLength)this.GetValue(HeightProperty); }
			set { this.SetValue(HeightProperty, value); }
		}

		public static readonly DependencyProperty HeightProperty =
			DependencyProperty.Register(
				"Height",
				typeof(GridLength),
				typeof(RowDefinition),
				new PropertyMetadata(
					GridLengthHelper.OneStar,
					(s, e) => ((RowDefinition)s)?.OnHeightChanged(e)
				)
			);

		private void OnHeightChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		public static implicit operator RowDefinition(string value)
		{
			return new RowDefinition { Height = GridLength.ParseGridLength(value).First() };
		}
	}
}