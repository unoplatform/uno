using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;
using Uno.Extensions;
using Private.Infrastructure;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Input.PointersTests
{
	[Sample("Pointers", "Geometry")]
	public sealed partial class HitTest_GeometryGroup : Page
	{
		public HitTest_GeometryGroup()
		{
			this.InitializeComponent();
			RegisterPointerEvents();

			Loaded += (s, e) =>
			{
				_ = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
				{
					// this needs to be added on SizeChanged or on a delay
					HollowCircle2.Data = GenerateHollowCircle(new Size(100, 100));
				});
			};
		}

		private GeometryGroup GenerateHollowCircle(Size size)
		{
			var center = new Point(size.Width / 2, size.Height / 2);
			var radius = Math.Min(size.Width, size.Height) / 2;
			var radius2 = radius * (1.0 / 2.0);

			return new GeometryGroup().Apply(x => x.Children.AddRange(new EllipseGeometry[]
			{
				new EllipseGeometry { Center = center, RadiusX = radius, RadiusY = radius },
				new EllipseGeometry { Center = center, RadiusX = radius2, RadiusY = radius2 },
			}));
		}

		private void RegisterPointerEvents()
		{
			foreach (var elt in GetElements())
			{
				elt.PointerPressed += (snd, e) =>
				{
					e.Handled = true;
					LastPressed.Text = elt.Name;
					LastPressedSrc.Text = (e.OriginalSource as FrameworkElement)?.Name ?? "-unknown-";
				};
				elt.PointerMoved += (snd, e) =>
				{
					e.Handled = true;
					LastHovered.Text = elt.Name;
					LastHoveredSrc.Text = (e.OriginalSource as FrameworkElement)?.Name ?? "-unknown-";
				};
			}
		}

		private IEnumerable<FrameworkElement> GetElements()
		{
			yield return HollowCircle1;
			yield return HollowCircle2;
		}
	}
}
