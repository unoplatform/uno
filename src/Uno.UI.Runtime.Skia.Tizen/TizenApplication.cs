using System.Threading;
using ElmSharp;
using SkiaSharp.Views.Tizen;
using Tizen.Applications;
using Tizen.NUI;
using Windows.UI.Core;
using TizenWindow = Tizen.NUI.Window;

namespace Uno.UI.Runtime.Skia.Tizen
{
	class TizenApplication : NUIApplication
	{
		private readonly TizenHost _host;

		public TizenApplication(TizenHost host)
		{
			_host = host;
		}		

		public UnoCanvas Canvas { get; set; }

		public TizenWindow AppWindow { get; private set; }

		protected override void OnPreCreate()
		{
			base.OnPreCreate();
			var synchronizationContext = SynchronizationContext.Current;
			Windows.UI.Core.CoreDispatcher.DispatchOverride = (d) => synchronizationContext!.Post((o) => d(), null);
			Windows.UI.Core.CoreDispatcher.HasThreadAccessOverride = () => synchronizationContext == SynchronizationContext.Current;
		}

		protected override void OnCreate()
		{
			base.OnCreate();
			EnsureMainWindow();
			Initialize();
			_host.PostRun();
		}

		private void EnsureMainWindow()
		{
			if (AppWindow != null)
			{
				return;
			}

			AppWindow = TizenWindow.Instance;
			//Window = new TizenWindow("ElmSharpApp")
			//{
			//	AvailableRotations =
			//		DisplayRotation.Degree_0 |
			//		DisplayRotation.Degree_180 |
			//		DisplayRotation.Degree_270 |
			//		DisplayRotation.Degree_90
			//};
			//Window.IndicatorMode = IndicatorMode.Hide;
			//Window.BackButtonPressed += (s, e) =>
			//{
			//	if (!SystemNavigationManager.GetForCurrentView().RequestBack())
			//	{
			//		Exit();
			//	}
			//};
			//Window.Show();

			Canvas = new UnoCanvas();
			Canvas.WidthResizePolicy = ResizePolicyType.FillToParent;
			Canvas.HeightResizePolicy = ResizePolicyType.FillToParent;

			TizenWindow.Instance.GetDefaultLayer().Add(Canvas);
			//TizenWindow.Instance.Show();
			//var conformant = new Conformant(Window);
			//conformant.Show();
			//conformant.SetContent(Canvas);
		}

		void Initialize()
		{
			//TizenWindow.Instance.KeyEvent += OnKeyEvent;

			//global::Tizen.NUI.BaseComponents.TextLabel text = new global::Tizen.NUI.BaseComponents.TextLabel("Hello Tizen")
			//{
			//	HorizontalAlignment = HorizontalAlignment.Center,
			//	VerticalAlignment = VerticalAlignment.Center,
			//	TextColor = global::Tizen.NUI.Color.Blue,
			//	PointSize = 12.0f,
			//	HeightResizePolicy = ResizePolicyType.FillToParent,
			//	WidthResizePolicy = ResizePolicyType.FillToParent
			//};
			//TizenWindow.Instance.GetDefaultLayer().Add(text);

			//Animation animation = new Animation(2000);
			//animation.AnimateTo(text, "Orientation", new Rotation(new Radian(new Degree(180.0f)), PositionAxis.X), 0, 500);
			//animation.AnimateTo(text, "Orientation", new Rotation(new Radian(new Degree(0.0f)), PositionAxis.X), 500, 1000);
			//animation.Looping = true;
			//animation.Play();
		}
	}
}
