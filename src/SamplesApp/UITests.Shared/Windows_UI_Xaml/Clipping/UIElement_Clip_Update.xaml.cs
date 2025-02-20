using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.Foundation;

namespace UITests.Windows_UI_Xaml.Clipping;


[Sample("Clipping", Description = """
	1. Initially, there should be a button and a 100x100 border.
	2. Click the button.
	3. The border should become smaller, 80x80
	4. Click the button once again
	5. The border should become smaller, 50x50
	""", IsManualTest = true)]
public sealed partial class UIElement_Clip_Update : Page
{
	private int _clickCount;

	public UIElement_Clip_Update()
	{
		this.InitializeComponent();
	}

	private void OnClick(object sender, RoutedEventArgs args)
	{
		if (_clickCount == 0)
		{
			SUT.Clip = new RectangleGeometry()
			{
				Rect = new Rect(0, 0, 80, 80),
			};
		}
		else if (_clickCount == 1)
		{
			((RectangleGeometry)SUT.Clip).Rect = new Rect(0, 0, 50, 50);
		}

		_clickCount++;
	}
}
