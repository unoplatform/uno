using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Data;
// using System.Windows.Markup; // <-- will cause MarkupWithArgsExtension to fail
// ^ as it would instead inherits from System.Windows.Markup rather than Windows.UI.Xaml.Markup

namespace XamlGenerationTests
{
	public partial class MarkupExtensionTests : UserControl
	{
		public MarkupExtensionTests()
		{
			InitializeComponent();
		}
	}
}

namespace XamlGenerationTests.MarkupExtensions
{
	public class NotImplementedConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}

	public class MarkupWithArgsExtension : MarkupExtension
	{
		public MarkupWithArgsExtension()
		{
		}
		public MarkupWithArgsExtension(object prop1, object prop2)
		{
			this.Prop1 = prop1;
			this.Prop2 = prop2;
		}

		[System.Windows.Markup.ConstructorArgument("prop1")]
		public object Prop1 { get; set; }

		[System.Windows.Markup.ConstructorArgument("prop2")]
		public object Prop2 { get; set; }

		protected override object ProvideValue() => this;
	}
}
