using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.App.Views
{
	public partial class MustNeverBeCreated : DependencyObject
	{
		public MustNeverBeCreated() => throw new InvalidOperationException("This type exists to validate the lazy initialization of resources in resource dictionaries. It must not be materialized.");

		public Brush BackForeground
		{
			get { return (Brush)GetValue(BackForegroundProperty); }
			set { SetValue(BackForegroundProperty, value); }
		}

		public static readonly DependencyProperty BackForegroundProperty =
			DependencyProperty.Register(nameof(BackForeground), typeof(Brush), typeof(MustNeverBeCreated), new PropertyMetadata(default));


	}
}
