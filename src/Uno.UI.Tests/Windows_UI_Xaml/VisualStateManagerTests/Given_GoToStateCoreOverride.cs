using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml.VisualStateManagerTests
{
	[TestClass]
	public class Given_GoToStateCoreOverride
	{
		[TestMethod]
		public void When_Inner_GoToState()
		{
			Border myBorder = null;
			MyPresenter myPresenter = null;

			var SUT = new MyItem()
			{
				Template = new ControlTemplate(() => myPresenter = new MyPresenter
				{
					Name = "myPresenter",
					Template = new ControlTemplate(() => new Grid
					{
						Children =
						{
							(myBorder = new Border(){ Name = "myBorder" })
						}
					}.Apply(g =>
					{
						var state1 = new VisualState
						{
							Name = "state1"
						};
						var state2 = new VisualState
						{
							Name = "state2",
							Setters =
							{
								new Setter(new TargetPropertyPath("myBorder", new PropertyPath("Tag")), "42")
							}
						};

						var group = new VisualStateGroup();
						group.States.Add(state1);
						group.States.Add(state2);

						VisualStateManager.SetVisualStateGroups(g, new List<VisualStateGroup>() { group });
					}))
				}
				.Apply(g =>
				{
					var state1 = new VisualState
					{
						Name = "state3"
					};
					var state2 = new VisualState
					{
						Name = "state4",
						Setters =
						{
							new Setter(new TargetPropertyPath("myPresenter", new PropertyPath("Tag")), "43")
						}
					};

					var group = new VisualStateGroup();
					group.States.Add(state1);
					group.States.Add(state2);

					VisualStateManager.SetVisualStateGroups(g, new List<VisualStateGroup>() { group });
				})
				)
			};

			SUT.ApplyTemplate();

			Assert.IsNotNull(myBorder);
			Assert.IsNull(myBorder.Tag);

			// Test that the outer presenter gets its own states set if
			// GoToElementStateCore does not override the default behavior
			VisualStateManager.GoToState(SUT, "state4", false);
			Assert.AreEqual("43", myPresenter.Tag);
			Assert.IsNull(myBorder.Tag);

			// Test that the inner presenter template root item gets its states set
			// when GoToElementStateCore overrides the default behavior.
			VisualStateManager.GoToState(SUT, "state2", false);
			Assert.AreEqual("42", myBorder.Tag);
			Assert.AreEqual("43", myPresenter.Tag);
		}
	}

	public class MyItem : ContentControl
	{
		public MyItem()
		{

		}
	}

	public class MyPresenter : ContentControl
	{
		public MyPresenter()
		{

		}

		protected override bool GoToElementStateCore(string stateName, bool useTransitions)
		{
			if (stateName == "state2")
			{
				return VisualStateManager.GoToState(this, stateName, useTransitions);
			}
			else
			{
				return base.GoToElementStateCore(stateName, useTransitions);
			}
		}
	}
}
