using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Toolkit
{
	partial class ElevatedView : Border
	{
		public ElevatedView()
		{
		}

		private static void OnChanged(DependencyObject snd, DependencyPropertyChangedEventArgs evt) => ((ElevatedView)snd).UpdateElevation();

		private void UpdateElevation()
		{
			this.SetElevationInternal(Background == null ? 0 : Elevation, ShadowColor);
		}
	}
}
