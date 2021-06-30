using ElmSharp;
using SkiaSharp.Views.Tizen;
using Tizen.Applications;
using Windows.UI.Core;
using TizenWindow = ElmSharp.Window;

namespace Uno.UI.Runtime.Skia.Tizen
{
	class TizenApplication : CoreUIApplication
	{
		private readonly TizenHost _host;

		public TizenApplication(TizenHost host)
		{
			_host = host;
		}

		public TizenWindow Window { get; set; }

		public UnoCanvas Canvas { get; set; }

		protected override void OnCreate()
		{
			base.OnCreate();
			EnsureMainWindow();
			_host.Run();
		}

		private void EnsureMainWindow()
		{
			if (Window != null)
			{
				return;
			}

			Window = new TizenWindow("ElmSharpApp")
			{
				AvailableRotations =
					DisplayRotation.Degree_0 |
					DisplayRotation.Degree_180 |
					DisplayRotation.Degree_270 |
					DisplayRotation.Degree_90
			};
			Window.IndicatorMode = IndicatorMode.Hide;
			Window.BackButtonPressed += (s, e) =>
			{
				if (!SystemNavigationManager.GetForCurrentView().RequestBack())
				{
					Exit();
				}
			};
			Window.Show();

			Canvas = new UnoCanvas(Window);
			Canvas.Show();

			var conformant = new Conformant(Window);
			conformant.Show();
			conformant.SetContent(Canvas);
		}
	}
}
