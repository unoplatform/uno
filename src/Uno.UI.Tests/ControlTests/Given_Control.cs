using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.ControlTests
{
	[TestClass]
	public partial class Given_Control
	{
		[TestMethod]
		public void When_ManualyApplyTemplate()
		{
			var templatedRoot = default(UIElement);
			var sut = new MyControl
			{
				Template = new ControlTemplate(() => templatedRoot = new Grid())
			};

			Assert.IsNull(sut.TemplatedRoot);
			Assert.IsNull(templatedRoot);

			new Grid().Children.Add(sut); // This kind-of simulate that the control is in the visual tree.

			Assert.IsNull(sut.TemplatedRoot);
			Assert.IsNull(templatedRoot);

			var applied = sut.ApplyTemplate();

			Assert.IsTrue(applied);
			Assert.IsNotNull(sut.TemplatedRoot);
			Assert.AreSame(templatedRoot, sut.TemplatedRoot);
		}

		[TestMethod]
		public void When_ManualyApplyTemplate_OutOfVisualTree()
		{
			var templatedRoot = default(UIElement);
			var sut = new MyControl
			{
				Template = new ControlTemplate(() => templatedRoot = new Grid())
			};

			Assert.IsNull(sut.TemplatedRoot);
			Assert.IsNull(templatedRoot);

			var applied = sut.ApplyTemplate();

			Assert.IsFalse(applied);
			Assert.IsNull(sut.TemplatedRoot);
			Assert.IsNull(templatedRoot);
		}

		public partial class MyControl : Control
		{
		}
	}
}
