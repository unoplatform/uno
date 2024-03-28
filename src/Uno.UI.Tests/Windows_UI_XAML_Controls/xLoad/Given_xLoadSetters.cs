using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls.xLoad
{
	[TestClass]
	public class Given_xLoadSetters
	{
		[TestMethod]
		public void When_ElementNameSetter()
		{
			var g = new Grid();
			Grid innerGrid = null;
			var subject = new ElementNameSubject(false, "test");
			var stub = new ElementStub
			{
				ContentBuilder = () =>
				{
					innerGrid = new Grid() { Name = "test" };
					subject.ElementInstance = innerGrid;
					return innerGrid;
				}
			};
			subject.ElementInstance = stub;

			g.Children.Add(stub);

			var SUT = new Setter(new TargetPropertyPath(subject, "Tag"), 42);

			var trigger = new StateTrigger() { IsActive = false };
			var vs = new VisualState { Setters = { SUT }, StateTriggers = { trigger } };

			var group = new VisualStateGroup();
			group.States.Add(vs);

			VisualStateManager.SetVisualStateGroups(g, new List<VisualStateGroup>() { group });

			Assert.IsNull(innerGrid);

			trigger.IsActive = true;
			Assert.IsNotNull(innerGrid);
			Assert.AreEqual(42, innerGrid.Tag);
		}

		[TestMethod]
		public void When_ElementNameSetter_Active()
		{
			var g = new Grid();
			Grid innerGrid = null;
			var subject = new ElementNameSubject(false, "test");
			var stub = new ElementStub
			{
				ContentBuilder = () =>
				{
					innerGrid = new Grid() { Name = "test" };
					subject.ElementInstance = innerGrid;
					return innerGrid;
				}
			};
			subject.ElementInstance = stub;

			g.Children.Add(stub);

			var SUT = new Setter(new TargetPropertyPath(subject, "Tag"), 42);

			var trigger = new StateTrigger() { IsActive = true };
			var vs = new VisualState { Setters = { SUT }, StateTriggers = { trigger } };

			var group = new VisualStateGroup();
			group.States.Add(vs);

			VisualStateManager.SetVisualStateGroups(g, new List<VisualStateGroup>() { group });

			Assert.AreEqual(42, innerGrid.Tag);
		}

		[TestMethod]
		public void When_ElementNameSubject_Lazy()
		{
			var ens = new ElementNameSubject(true, "test");

			var g = new Grid();
			Border b1 = null;
			var stub = new ElementStub
			{
				Name = "test",
				ContentBuilder = () =>
				{
					b1 = new Border() { Name = "test" };
					ens.ElementInstance = b1;
					return b1;
				}
			};

			ens.ElementInstance = stub;

			g.Children.Add(stub);

			Assert.IsNull(ens.ElementInstance);

			var b2 = g.FindName("test");
			Assert.IsNotNull(b2);
			Assert.AreEqual(b1, b2);
			Assert.AreEqual(b1, g.Children.First());
		}
	}
}
