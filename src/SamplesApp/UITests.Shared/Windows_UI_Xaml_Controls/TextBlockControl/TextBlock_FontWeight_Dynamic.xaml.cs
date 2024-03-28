using System.Collections.Generic;
using System.Reflection;
using Uno.UI.Samples.Controls;
using Windows.UI.Text;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.TextBlockControl
{
	[Sample("TextBlock")]
	public sealed partial class TextBlock_FontWeight_Dynamic : UserControl
	{
		int CurrentIndex;
		List<FontWeight> Weights;

		public TextBlock_FontWeight_Dynamic()
		{
			this.InitializeComponent();
			this.InitWeights();
		}

		private void InitWeights()
		{
			CurrentIndex = 0;
			Weights = new List<FontWeight>();
			Weights.Add(FontWeights.Thin);
			Weights.Add(FontWeights.Light);
			Weights.Add(FontWeights.Normal);
			Weights.Add(FontWeights.Bold);
			Weights.Add(FontWeights.Black);
		}

		private void OnClick(object sender, object args)
		{
			ChangeFontWeight();
		}

		private void ChangeFontWeight()
		{
			int nextIndex = (CurrentIndex + 1) % Weights.Count;
			FontWeight nextWeight = Weights[nextIndex];
			testBlock.FontWeight = nextWeight;
			testBlock.Text = $"Font weight changed to {nextWeight.Weight}";
			CurrentIndex = nextIndex;
		}

	}
}
