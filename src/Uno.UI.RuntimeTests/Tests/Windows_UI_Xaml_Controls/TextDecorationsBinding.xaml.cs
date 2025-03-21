using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public sealed partial class TextDecorationsBinding : Page
{
	public int P1 => (int)TextDecorations.Strikethrough;
	public uint P2 => (uint)TextDecorations.Underline;
	public TextDecorations P3 => TextDecorations.Strikethrough;
	public TextDecorationsBinding P4 => null;
	public string P5 => "Strikethrough";
	public string P6 => "InvalidValue";

	public TextDecorationsBinding()
	{
		this.InitializeComponent();
		DataContext = this;
	}
}
