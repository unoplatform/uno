using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Shapes;

namespace Uno.UI.Tests
{
	// BC38: Background is declared independently on Control, Panel, Border, ContentPresenter,
	// ItemsRepeater (and ScrollPresenter/SwipeItem), matching WinUI. These tests guard the
	// property-name resolution across the type hierarchy now that there is no single
	// FrameworkElement-level Background.
	[TestClass]
	public class Given_DependencyProperty_BackgroundOwnership
	{
		[TestMethod]
		public void When_Resolved_On_Subclass_Returns_Nearest_Declaring_Type()
		{
			// Panel subclasses resolve to Panel.BackgroundProperty
			Assert.AreSame(Panel.BackgroundProperty, DependencyProperty.GetProperty(typeof(Grid), "Background"));
			Assert.AreSame(Panel.BackgroundProperty, DependencyProperty.GetProperty(typeof(StackPanel), "Background"));

			// Control subclasses resolve to Control.BackgroundProperty
			Assert.AreSame(Control.BackgroundProperty, DependencyProperty.GetProperty(typeof(Button), "Background"));
			Assert.AreSame(Control.BackgroundProperty, DependencyProperty.GetProperty(typeof(ContentControl), "Background"));

			// ContentPresenter subclasses resolve to ContentPresenter.BackgroundProperty
			Assert.AreSame(ContentPresenter.BackgroundProperty, DependencyProperty.GetProperty(typeof(ScrollContentPresenter), "Background"));
			Assert.AreSame(ContentPresenter.BackgroundProperty, DependencyProperty.GetProperty(typeof(ListViewItemPresenter), "Background"));

			// The declaring types resolve to themselves
			Assert.AreSame(Border.BackgroundProperty, DependencyProperty.GetProperty(typeof(Border), "Background"));
			Assert.AreSame(ItemsRepeater.BackgroundProperty, DependencyProperty.GetProperty(typeof(ItemsRepeater), "Background"));
		}

		[TestMethod]
		public void When_Resolved_On_NonPainter_Returns_Null()
		{
			// WinUI parity: these types do not expose Background at all.
			Assert.IsNull(DependencyProperty.GetProperty(typeof(TextBlock), "Background"));
			Assert.IsNull(DependencyProperty.GetProperty(typeof(RichTextBlock), "Background"));
			Assert.IsNull(DependencyProperty.GetProperty(typeof(Image), "Background"));
			Assert.IsNull(DependencyProperty.GetProperty(typeof(Rectangle), "Background"));
			Assert.IsNull(DependencyProperty.GetProperty(typeof(Ellipse), "Background"));
		}

		[TestMethod]
		public void When_Painters_Then_Independent_Properties_And_Owners()
		{
			// Each painter owns a distinct property instance...
			Assert.AreNotSame(Control.BackgroundProperty, Panel.BackgroundProperty);
			Assert.AreNotSame(Control.BackgroundProperty, Border.BackgroundProperty);
			Assert.AreNotSame(Control.BackgroundProperty, ContentPresenter.BackgroundProperty);
			Assert.AreNotSame(Panel.BackgroundProperty, Border.BackgroundProperty);

			// ...registered against its own owner type.
			Assert.AreEqual(typeof(Control), Control.BackgroundProperty.OwnerType);
			Assert.AreEqual(typeof(Panel), Panel.BackgroundProperty.OwnerType);
			Assert.AreEqual(typeof(Border), Border.BackgroundProperty.OwnerType);
			Assert.AreEqual(typeof(ContentPresenter), ContentPresenter.BackgroundProperty.OwnerType);
			Assert.AreEqual(typeof(ItemsRepeater), ItemsRepeater.BackgroundProperty.OwnerType);
		}
	}
}
