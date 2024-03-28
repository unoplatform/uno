using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.App.Views
{
	public partial class ResourceTestControl : Control
	{
		public TextBlock TextBlock1 { get; private set; }
		public TextBlock TextBlock2 { get; private set; }
		public TextBlock TextBlock3 { get; private set; }
		public TextBlock TextBlock4 { get; private set; }
		public TextBlock TextBlock5 { get; private set; }
		public TextBlock TextBlock6 { get; private set; }

		public MyPoco Poco { get; set; }

		public MyDependencyObject PlainDO { get; set; }

		public string MyString { get; set; }

		public ResourceTestControl()
		{
			DefaultStyleKey = typeof(ResourceTestControl);
		}

		public string MyStringDP
		{
			get { return (string)GetValue(MyStringDPProperty); }
			set { SetValue(MyStringDPProperty, value); }
		}

		public static readonly DependencyProperty MyStringDPProperty =
			DependencyProperty.Register("MyStringDP", typeof(string), typeof(ResourceTestControl), new PropertyMetadata("DefaultValue"));

		public MyDependencyObject DObjDP
		{
			get { return (MyDependencyObject)GetValue(DObjDPProperty); }
			set { SetValue(DObjDPProperty, value); }
		}

		public static readonly DependencyProperty DObjDPProperty =
			DependencyProperty.Register("DObjDP", typeof(MyDependencyObject), typeof(ResourceTestControl), new PropertyMetadata(null));



		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			TextBlock1 = GetTemplateChild(nameof(TextBlock1)) as TextBlock;
			TextBlock2 = GetTemplateChild(nameof(TextBlock2)) as TextBlock;
			TextBlock3 = GetTemplateChild(nameof(TextBlock3)) as TextBlock;
			TextBlock4 = GetTemplateChild(nameof(TextBlock4)) as TextBlock;
			TextBlock5 = GetTemplateChild(nameof(TextBlock5)) as TextBlock;
			TextBlock6 = GetTemplateChild(nameof(TextBlock6)) as TextBlock;
		}
	}
}
