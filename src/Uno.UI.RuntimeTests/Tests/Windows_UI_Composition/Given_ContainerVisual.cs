using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Private.Infrastructure;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

#if __SKIA__
[TestClass]
public class Given_ContainerVisual
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Children_Change()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var containerVisual = compositor.CreateContainerVisual();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);

		var shape = compositor.CreateShapeVisual();
		containerVisual.Children.InsertAtTop(shape);
		Assert.IsTrue(containerVisual.IsChildrenRenderOrderDirty);
		var children = containerVisual.GetChildrenInRenderOrderTestingOnly();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);
		Assert.AreEqual(1, children.Count());

		containerVisual.Children.InsertAtTop(compositor.CreateShapeVisual());
		Assert.IsTrue(containerVisual.IsChildrenRenderOrderDirty);
		children = containerVisual.GetChildrenInRenderOrderTestingOnly();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);
		Assert.AreEqual(2, children.Count());

		containerVisual.Children.Remove(shape);
		Assert.IsTrue(containerVisual.IsChildrenRenderOrderDirty);
		children = containerVisual.GetChildrenInRenderOrderTestingOnly();
		Assert.IsFalse(containerVisual.IsChildrenRenderOrderDirty);
		Assert.AreEqual(1, children.Count());
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Child_Removed_TotalMatrix_Updated()
	{
		var compositor = TestServices.WindowHelper.XamlRoot.Compositor;
		var parent1 = compositor.CreateContainerVisual();
		var parent2 = compositor.CreateContainerVisual();
		var child = compositor.CreateSpriteVisual();

		parent2.Offset = new Vector3(100, 0, 0);

		parent1.Children.InsertAtTop(child);
		Assert.IsTrue(child.TotalMatrix.IsIdentity);

		parent2.Children.InsertAtTop(child);
		Assert.IsFalse(child.TotalMatrix.IsIdentity);
	}
}
#endif
