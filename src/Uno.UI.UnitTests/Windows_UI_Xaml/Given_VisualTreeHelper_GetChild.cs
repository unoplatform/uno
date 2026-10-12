using System;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_VisualTreeHelper_GetChild
	{
		[TestInitialize]
		public void Init() => UnitTestsApp.App.EnsureApplication();

		private static ElementStub NewStub() => new(() => new Border());

		private static void AssertWalk(StackPanel panel, params UIElement[] expected)
		{
			Assert.AreEqual(expected.Length, VisualTreeHelper.GetChildrenCount(panel));
			for (var i = 0; i < expected.Length; i++)
			{
				Assert.AreSame(expected[i], VisualTreeHelper.GetChild(panel, i));
			}

			Assert.IsNull(VisualTreeHelper.GetChild(panel, expected.Length));
			Assert.IsNull(VisualTreeHelper.GetChild(panel, -1));
		}

		[TestMethod]
		public void When_NoStubs()
		{
			var panel = new StackPanel();
			var a = new Border();
			var b = new Border();
			panel.Children.Add(a);
			panel.Children.Add(b);
			AssertWalk(panel, a, b);

			var c = new Border();
			panel.Children.Insert(1, c);
			AssertWalk(panel, a, c, b);

			panel.Children.Remove(c);
			AssertWalk(panel, a, b);

			panel.Children.Clear();
			AssertWalk(panel);
		}

		[TestMethod]
		public void When_StubsSkipped()
		{
			var panel = new StackPanel();
			var a = new Border();
			var b = new Border();
			var stub = NewStub();
			panel.Children.Add(stub);
			panel.Children.Add(a);
			panel.Children.Add(NewStub());
			panel.Children.Add(b);
			AssertWalk(panel, a, b);

			panel.Children.Remove(stub);
			AssertWalk(panel, a, b);
		}

		[TestMethod]
		public void When_StubMaterialized()
		{
			var panel = new StackPanel();
			var a = new Border();
			var stub = NewStub();
			panel.Children.Add(a);
			panel.Children.Add(stub);
			AssertWalk(panel, a);

			stub.Load = true;
			Assert.AreEqual(2, VisualTreeHelper.GetChildrenCount(panel));
			Assert.AreSame(a, VisualTreeHelper.GetChild(panel, 0));
			Assert.IsNotNull(VisualTreeHelper.GetChild(panel, 1));
			Assert.IsNotInstanceOfType<ElementStub>(VisualTreeHelper.GetChild(panel, 1));
		}

		[TestMethod]
		public void When_StubsCleared_ThenFastPath()
		{
			var panel = new StackPanel();
			panel.Children.Add(NewStub());
			panel.Children.Clear();
			var a = new Border();
			panel.Children.Add(a);
			AssertWalk(panel, a);
		}

		[TestMethod]
		public void When_WidePanel_WalkIsNotQuadratic()
		{
			var panel = new StackPanel();
			for (var i = 0; i < 2000; i++)
			{
				panel.Children.Add(new Border());
			}

			for (var warm = 0; warm < 3; warm++)
			{
				Walk(panel);
			}

			var sw = Stopwatch.StartNew();
			const int Iterations = 20;
			for (var i = 0; i < Iterations; i++)
			{
				Walk(panel);
			}

			sw.Stop();
			var nsPerChild = sw.Elapsed.TotalMilliseconds * 1_000_000 / (Iterations * 2000);
			Console.WriteLine($"GetChild walk: {nsPerChild:F0} ns per child");

			// Generous: the scanning path costs microseconds per child at this width.
			Assert.IsLessThan(1000, nsPerChild);
		}

		private static void Walk(StackPanel panel)
		{
			var count = VisualTreeHelper.GetChildrenCount(panel);
			for (var i = 0; i < count; i++)
			{
				_ = VisualTreeHelper.GetChild(panel, i);
			}
		}
	}
}
