using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Composition;

[Sample("Microsoft.UI.Composition")]
public sealed partial class VisualTranslationSample : Page
{
	private readonly Visual _border2Visual;

	public VisualTranslationSample()
	{
		this.InitializeComponent();
		_border2Visual = ElementCompositionPreview.GetElementVisual(secondBorder);
		this.Loaded += (_, _) =>
		{
			ElementCompositionPreview.SetIsTranslationEnabled(secondBorder, true);
			_border2Visual.Properties.InsertVector3("Translation", new Vector3(100, 0, 0));
		};
	}
}
