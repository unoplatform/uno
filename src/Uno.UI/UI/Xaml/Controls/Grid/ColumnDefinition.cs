using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class ColumnDefinition : DependencyObject
	{
		public ColumnDefinition()
		{
			InitializeBinder();
			IsAutoPropertyInheritanceEnabled = false;
		}

		#region Width DependencyProperty

		public GridLength Width
		{
			get { return (GridLength)this.GetValue(WidthProperty); }
			set { this.SetValue(WidthProperty, value); }
		}

		public static readonly DependencyProperty WidthProperty =
			DependencyProperty.Register(
				"Width",
				typeof(GridLength),
				typeof(ColumnDefinition),
				new PropertyMetadata(
					GridLengthHelper.OneStar,
					(s, e) => ((ColumnDefinition)s)?.OnWidthChanged(e)
				)
			);

		private void OnWidthChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		public static implicit operator ColumnDefinition(string value)
		{
			return new ColumnDefinition { Width = GridLength.ParseGridLength(value).First() };
		}
	}
}