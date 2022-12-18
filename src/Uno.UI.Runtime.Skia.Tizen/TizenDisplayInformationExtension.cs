using Tizen.NUI;
using Windows.Graphics.Display;
#pragma warning disable CS0649
namespace Uno.UI.Runtime.Skia
{
	internal class TizenDisplayInformationExtension : IDisplayInformationExtension
	{
		private DisplayInformation _displayInformation;
		private readonly string _profile;

		private Window _window;		
		private int? _dpi;

		public TizenDisplayInformationExtension(object owner, Window window)
		{
			_displayInformation = (DisplayInformation)owner;
			_window = window;
			//_profile = Elementary.GetProfile();
		}

		public DisplayOrientations CurrentOrientation => DisplayOrientations.Portrait;

		public uint ScreenHeightInRawPixels => (uint)_window.WindowSize.Height;

		public uint ScreenWidthInRawPixels => (uint)_window.WindowSize.Width;

		public float LogicalDpi => _dpi ??= GetDpi();

		public double RawPixelsPerViewPixel => 1;

		public ResolutionScale ResolutionScale => (ResolutionScale)(int)(LogicalDpi / 1.60f);

		public double? DiagonalSizeInInches => null;

		private int GetDpi()
		{
			// TV has fixed DPI value (72)
			if (_profile == "tv")
			{
				return 72;
			}

#pragma warning disable CS0618 // Type or member is obsolete
			// TODO:MZ:
			//SystemInfo.TryGetValue("http://tizen.org/feature/screen.dpi", out int dpi);
#pragma warning restore CS0618 // Type or member is obsolete
			return 96;
		}
	}
}
