#if NETFX_CORE || NET46
namespace Uno.UI.Toolkit
{
	partial class ElevatedView : Border
	{
		private static void OnChanged(DependencyObject snd, DependencyPropertyChangedEventArgs evt) => ((ElevatedView)snd).UpdateElevation();

		private void UpdateElevation()
		{
			this.SetElevationInternal(Background == null ? 0 : Elevation, ShadowColor);
		}
	}
}
#endif
