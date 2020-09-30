using System.Windows.Controls;
using WpfFrameworkPropertyMetadata = System.Windows.FrameworkPropertyMetadata;

namespace Uno.UI.Runtime.Skia.WPF.Controls
{
	public class WpfTextViewTextBox : TextBox
	{
		static WpfTextViewTextBox()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(WpfTextViewTextBox),
				new WpfFrameworkPropertyMetadata(typeof(WpfTextViewTextBox)));
		}

		public override void OnApplyTemplate() => base.OnApplyTemplate();
	}
}
