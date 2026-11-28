#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[UITestClass]
public class Given_UIElement_ChildDesiredSizeChanged
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24676")]
	public void When_Child_DesiredSize_Changes_Parent_Receives_Child()
	{
		var panel = new RecordingPanel();
		var child = new Border { Width = 10, Height = 10 };
		panel.Children.Add(child);

		panel.Measure(new Size(100, 100));
		panel.ReceivedChildren.Clear();

		child.Width = 20;
		child.Measure(new Size(100, 100));

		Assert.AreEqual(new Size(20, 10), child.DesiredSize);
		Assert.AreEqual(1, panel.ReceivedChildren.Count);
		Assert.AreSame(child, panel.ReceivedChildren[0]);
	}

	private partial class RecordingPanel : Panel
	{
		public List<UIElement> ReceivedChildren { get; } = new();

		private protected override void OnChildDesiredSizeChanged(UIElement child)
		{
			ReceivedChildren.Add(child);
			base.OnChildDesiredSizeChanged(child);
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			var width = 0d;
			var height = 0d;

			foreach (var child in Children)
			{
				child.Measure(availableSize);
				width = Math.Max(width, child.DesiredSize.Width);
				height = Math.Max(height, child.DesiredSize.Height);
			}

			return new Size(width, height);
		}
	}
}
