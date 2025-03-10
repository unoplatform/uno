using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using System;
using System.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Uno.Disposables;
using Private.Infrastructure;

namespace UITests.Windows_UI_Xaml.UIElementTests
{
	[Sample("UIElement", Description = "This is an illustration of GH Bug #3519")]
	public sealed partial class UIElement_InitializationSequence : Page
	{
		public UIElement_InitializationSequence()
		{
			Instance = this;

			InitializeComponent();

			DataContext = "[myContext]";
			var inner = new UIElement_InitializationSequence_TestControl(Log, "inner")
			{
				Style = Resources["testControlStyle"] as Style
			};
			var outer = new UIElement_InitializationSequence_TestControl(Log, "outer")
			{
				Content = inner,
				Style = Resources["testControlStyle"] as Style
			};
			testZone.Child = outer;

			reference.Text = @"
inner.ctor
inner.OnWidthChanged: 
inner.OnTagChanged: tag-value
outer.ctor
outer.OnWidthChanged: 
outer.OnTagChanged: tag-value
outer.DataContextChanged: [myContext]
outer.DataContextChanged: [myContext]
outer.Loading
ControlTemplate materialized.
outer.OnApplyTemplate()
outer.MeasureOverride()
-	>inner.DataContextChanged: [myContext]
-	>inner.Loading
-	>ControlTemplate materialized.
-	>inner.OnApplyTemplate()
-	>inner.MeasureOverride()
outer.ArrangeOverride()
-	>inner.ArrangeOverride()
outer.SizeChanged
inner.SizeChanged
outer.Loaded
inner.Loaded";
		}

		private int recursivityLevel = 0;

		internal IDisposable Log(string s)
		{
			var spacer = string.Concat(Enumerable.Repeat("-\t>", recursivityLevel++));

			var t = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () => { log.Text += "\n" + spacer + s; });

			return Disposable.Create(() => recursivityLevel--);
		}

		internal static UIElement_InitializationSequence Instance;
	}
}
