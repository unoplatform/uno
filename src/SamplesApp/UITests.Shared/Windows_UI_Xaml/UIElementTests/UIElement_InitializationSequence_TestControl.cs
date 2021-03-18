using System;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace UITests.Windows_UI_Xaml.UIElementTests
{
	public sealed partial class UIElement_InitializationSequence_TestControl : ContentControl
	{
		private readonly Func<string, IDisposable> _logger;

		public UIElement_InitializationSequence_TestControl(Func<string, IDisposable> logger, string name)
		{
			_logger = logger;
			Name = name;
			using (LogRecursively("ctor"))
			{
				DefaultStyleKey = typeof(UIElement_InitializationSequence_TestControl);

				Loaded += (snd, evt) => Log("Loaded");
				Loading += (snd, evt) => Log("Loading");
				//LayoutUpdated += (snd, evt) => Log("LayoutUpdated");
				DataContextChanged += (snd, evt) => Log("DataContextChanged: " + DataContext);
				Unloaded += (snd, evt) => Log("Unloaded");
				SizeChanged += (snd, evt) => Log("SizeChanged");

				RegisterPropertyChangedCallback(WidthProperty, (o, p) => Log("OnWidthChanged: " + Tag));
				RegisterPropertyChangedCallback(TagProperty, (o, p) => Log("OnTagChanged: " + Tag));
			}
		}

		private IDisposable LogRecursively(string s)
		{
			return _logger(Name + "." + s);
		}

		private void Log(string s)
		{
			LogRecursively(s).Dispose();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			using (LogRecursively("MeasureOverride()"))
			{
				return base.MeasureOverride(availableSize);
			}
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			using (LogRecursively("ArrangeOverride()"))
			{
				return base.ArrangeOverride(finalSize);
			}
		}

		protected override void OnApplyTemplate()
		{
			using (LogRecursively("OnApplyTemplate()"))
			{
				base.OnApplyTemplate();
			}
		}
	}
}
