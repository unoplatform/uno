#if (HAS_UNO || HAS_UNO_WINUI) && !WINAPPSDK
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RelativePanelUnitTests.cs

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MUXControlsTestApp.Utilities;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Windows_UI_Xaml_Controls
{
	[RunsOnUIThread]
	[TestClass]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
	public class RelativePanelUnitTests
	{
		private RPNode AddNodeToGraph(
			RPGraph graph,
			UIElement element,
			float width,
			float height)
		{
			LayoutInformation.SetDesiredSize(element, new Size(width, height));

			var node = new RPNode(element);
			graph.GetNodes().AddFirst(node);

			return graph.GetNodes().First.Value;
		}

		[TestMethod]
		public void VerifyHorizontalDependencyResolution()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 300, 100);

			node0.SetAlignHorizontalCenterWithConstraint(node1);
			node1.SetAlignTopWithPanelConstraint(true);

			{
				Rect finalRect = new Rect(0, 0, 300, 100);
				Rect mr0 = new Rect(0, 0, 300, 100);
				Rect mr1 = new Rect(0, 0, 300, 100);
				Rect ar0 = new Rect(100, 0, 100, 100);
				Rect ar1 = new Rect(0, 0, 300, 100);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			}
		}

		[TestMethod]
		public void VerifyVerticalDependencyResolution()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 300);

			node0.SetAlignVerticalCenterWithConstraint(node1);
			node1.SetAlignTopWithPanelConstraint(true);

			{
				Rect finalRect = new Rect(0, 0, 100, 300);
				Rect mr0 = new Rect(0, 0, 100, 300);
				Rect mr1 = new Rect(0, 0, 100, 300);
				Rect ar0 = new Rect(0, 100, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 300);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			}
		}

		[TestMethod]
		public void VerifyDownwardPrecedence()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
			RPNode node2 = AddNodeToGraph(graph, e2, 100, 50);

			node0.SetAlignTopWithPanelConstraint(true);
			node1.SetBelowConstraint(node0);
			node2.SetAlignVerticalCenterWithPanelConstraint(true);
			node2.SetBelowConstraint(node1);
			node2.SetAlignVerticalCenterWithConstraint(node1);
			node2.SetAlignTopWithConstraint(node1);
			node2.SetAlignTopWithPanelConstraint(true);

			{
				Rect finalRect = new Rect(0, 0, 100, 200);
				Rect mr0 = new Rect(0, 0, 100, 200);
				Rect mr1 = new Rect(0, 100, 100, 100);
				Rect mr2 = new Rect(0, 0, 100, 200);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 100);
				Rect ar2 = new Rect(0, 0, 100, 50);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 200);
				Rect mr0 = new Rect(0, 0, 100, 200);
				Rect mr1 = new Rect(0, 100, 100, 100);
				Rect mr2 = new Rect(0, 100, 100, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 100);
				Rect ar2 = new Rect(0, 100, 100, 50);

				node2.SetAlignTopWithPanelConstraint(false);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 200);
				Rect mr0 = new Rect(0, 0, 100, 200);
				Rect mr1 = new Rect(0, 100, 100, 100);
				Rect mr2 = new Rect(0, 100, 100, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 100);
				Rect ar2 = new Rect(0, 125, 100, 50);

				node2.SetAlignTopWithConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 250);
				Rect mr0 = new Rect(0, 0, 100, 250);
				Rect mr1 = new Rect(0, 100, 100, 150);
				Rect mr2 = new Rect(0, 200, 100, 50);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 100);
				Rect ar2 = new Rect(0, 200, 100, 50);

				node2.SetAlignVerticalCenterWithConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 200);
				Rect mr0 = new Rect(0, 0, 100, 200);
				Rect mr1 = new Rect(0, 100, 100, 100);
				Rect mr2 = new Rect(0, 0, 100, 200);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 100);
				Rect ar2 = new Rect(0, 75, 100, 50);

				node2.SetBelowConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}
		}

		[TestMethod]
		public void VerifyUpwardPrecedence()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
			RPNode node2 = AddNodeToGraph(graph, e2, 100, 50);

			node0.SetAlignBottomWithPanelConstraint(true);
			node1.SetAboveConstraint(node0);
			node2.SetAlignVerticalCenterWithPanelConstraint(true);
			node2.SetAboveConstraint(node1);
			node2.SetAlignVerticalCenterWithConstraint(node1);
			node2.SetAlignBottomWithConstraint(node1);
			node2.SetAlignBottomWithPanelConstraint(true);

			{
				Rect finalRect = new Rect(0, 0, 100, 200);
				Rect mr0 = new Rect(0, 0, 100, 200);
				Rect mr1 = new Rect(0, 0, 100, 100);
				Rect mr2 = new Rect(0, 0, 100, 200);
				Rect ar0 = new Rect(0, 100, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 100);
				Rect ar2 = new Rect(0, 150, 100, 50);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 200);
				Rect mr0 = new Rect(0, 0, 100, 200);
				Rect mr1 = new Rect(0, 0, 100, 100);
				Rect mr2 = new Rect(0, 0, 100, 100);
				Rect ar0 = new Rect(0, 100, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 100);
				Rect ar2 = new Rect(0, 50, 100, 50);

				node2.SetAlignBottomWithPanelConstraint(false);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 200);
				Rect mr0 = new Rect(0, 0, 100, 200);
				Rect mr1 = new Rect(0, 0, 100, 100);
				Rect mr2 = new Rect(0, 0, 100, 100);
				Rect ar0 = new Rect(0, 100, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 100);
				Rect ar2 = new Rect(0, 25, 100, 50);

				node2.SetAlignBottomWithConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 250);
				Rect mr0 = new Rect(0, 0, 100, 250);
				Rect mr1 = new Rect(0, 0, 100, 150);
				Rect mr2 = new Rect(0, 0, 100, 50);
				Rect ar0 = new Rect(0, 150, 100, 100);
				Rect ar1 = new Rect(0, 50, 100, 100);
				Rect ar2 = new Rect(0, 0, 100, 50);

				node2.SetAlignVerticalCenterWithConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 200);
				Rect mr0 = new Rect(0, 0, 100, 200);
				Rect mr1 = new Rect(0, 0, 100, 100);
				Rect mr2 = new Rect(0, 0, 100, 200);
				Rect ar0 = new Rect(0, 100, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 100);
				Rect ar2 = new Rect(0, 75, 100, 50);

				node2.SetAboveConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}
		}

		[TestMethod]
		public void VerifyRightwardPrecedence()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
			RPNode node2 = AddNodeToGraph(graph, e2, 50, 100);

			node0.SetAlignLeftWithPanelConstraint(true);
			node1.SetRightOfConstraint(node0);
			node2.SetAlignHorizontalCenterWithPanelConstraint(true);
			node2.SetRightOfConstraint(node1);
			node2.SetAlignHorizontalCenterWithConstraint(node1);
			node2.SetAlignLeftWithConstraint(node1);
			node2.SetAlignLeftWithPanelConstraint(true);

			{
				Rect finalRect = new Rect(0, 0, 200, 100);
				Rect mr0 = new Rect(0, 0, 200, 100);
				Rect mr1 = new Rect(100, 0, 100, 100);
				Rect mr2 = new Rect(0, 0, 200, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 100, 100);
				Rect ar2 = new Rect(0, 0, 50, 100);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 200, 100);
				Rect mr0 = new Rect(0, 0, 200, 100);
				Rect mr1 = new Rect(100, 0, 100, 100);
				Rect mr2 = new Rect(100, 0, 100, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 100, 100);
				Rect ar2 = new Rect(100, 0, 50, 100);

				node2.SetAlignLeftWithPanelConstraint(false);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 200, 100);
				Rect mr0 = new Rect(0, 0, 200, 100);
				Rect mr1 = new Rect(100, 0, 100, 100);
				Rect mr2 = new Rect(100, 0, 100, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 100, 100);
				Rect ar2 = new Rect(125, 0, 50, 100);

				node2.SetAlignLeftWithConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 250, 100);
				Rect mr0 = new Rect(0, 0, 250, 100);
				Rect mr1 = new Rect(100, 0, 150, 100);
				Rect mr2 = new Rect(200, 0, 50, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 100, 100);
				Rect ar2 = new Rect(200, 0, 50, 100);

				node2.SetAlignHorizontalCenterWithConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 200, 100);
				Rect mr0 = new Rect(0, 0, 200, 100);
				Rect mr1 = new Rect(100, 0, 100, 100);
				Rect mr2 = new Rect(0, 0, 200, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 100, 100);
				Rect ar2 = new Rect(75, 0, 50, 100);

				node2.SetRightOfConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}
		}

		[TestMethod]
		public void VerifyLeftwardPrecedence()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
			RPNode node2 = AddNodeToGraph(graph, e2, 50, 100);

			node0.SetAlignRightWithPanelConstraint(true);
			node1.SetLeftOfConstraint(node0);
			node2.SetAlignHorizontalCenterWithPanelConstraint(true);
			node2.SetLeftOfConstraint(node1);
			node2.SetAlignHorizontalCenterWithConstraint(node1);
			node2.SetAlignRightWithConstraint(node1);
			node2.SetAlignRightWithPanelConstraint(true);

			{
				Rect finalRect = new Rect(0, 0, 200, 100);
				Rect mr0 = new Rect(0, 0, 200, 100);
				Rect mr1 = new Rect(0, 0, 100, 100);
				Rect mr2 = new Rect(0, 0, 200, 100);
				Rect ar0 = new Rect(100, 0, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 100);
				Rect ar2 = new Rect(150, 0, 50, 100);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 200, 100);
				Rect mr0 = new Rect(0, 0, 200, 100);
				Rect mr1 = new Rect(0, 0, 100, 100);
				Rect mr2 = new Rect(0, 0, 100, 100);
				Rect ar0 = new Rect(100, 0, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 100);
				Rect ar2 = new Rect(50, 0, 50, 100);

				node2.SetAlignRightWithPanelConstraint(false);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 200, 100);
				Rect mr0 = new Rect(0, 0, 200, 100);
				Rect mr1 = new Rect(0, 0, 100, 100);
				Rect mr2 = new Rect(0, 0, 100, 100);
				Rect ar0 = new Rect(100, 0, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 100);
				Rect ar2 = new Rect(25, 0, 50, 100);

				node2.SetAlignRightWithConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 250, 100);
				Rect mr0 = new Rect(0, 0, 250, 100);
				Rect mr1 = new Rect(0, 0, 150, 100);
				Rect mr2 = new Rect(0, 0, 50, 100);
				Rect ar0 = new Rect(150, 0, 100, 100);
				Rect ar1 = new Rect(50, 0, 100, 100);
				Rect ar2 = new Rect(0, 0, 50, 100);

				node2.SetAlignHorizontalCenterWithConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 200, 100);
				Rect mr0 = new Rect(0, 0, 200, 100);
				Rect mr1 = new Rect(0, 0, 100, 100);
				Rect mr2 = new Rect(0, 0, 200, 100);
				Rect ar0 = new Rect(100, 0, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 100);
				Rect ar2 = new Rect(75, 0, 50, 100);

				node2.SetLeftOfConstraint(null);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}
		}

		[TestMethod]
		public void VerifyVerticallyFloatingAndOverlapping()
		{
			Size availableSize = new Size(2000.0f, 2000.0f);
			Rect finalRect = new Rect(0, 0, 100, 200);
			Rect mr0 = new Rect(0, 0, 100, 200);
			Rect mr1 = new Rect(0, 0, 100, 200);
			Rect mr2 = new Rect(0, 0, 100, 200);
			Rect mr3 = new Rect(0, 0, 100, 200);
			Rect ar0 = new Rect(0, 0, 100, 200);
			Rect ar1 = new Rect(0, 0, 100, 100);
			Rect ar2 = new Rect(0, 100, 100, 100);
			Rect ar3 = new Rect(0, 50, 100, 100);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100.0f, 200.0f);
			RPNode node1 = AddNodeToGraph(graph, e1, 100.0f, 100.0f);
			RPNode node2 = AddNodeToGraph(graph, e2, 100.0f, 100.0f);
			RPNode node3 = AddNodeToGraph(graph, e3, 100.0f, 100.0f);

			node1.SetAlignTopWithPanelConstraint(true);
			node2.SetAlignBottomWithPanelConstraint(true);
			node3.SetAlignVerticalCenterWithPanelConstraint(true);

			graph.MeasureNodes(availableSize);
			panelSize = graph.CalculateDesiredSize();
			graph.ArrangeNodes(finalRect);

			VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
			VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
			VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
			VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
			VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
			VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
			VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
			VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
		}

		[TestMethod]
		public void VerifyHorizontallyFloatingAndOverlapping()
		{
			Size availableSize = new Size(2000.0f, 2000.0f);
			Rect finalRect = new Rect(0, 0, 200, 100);
			Rect mr0 = new Rect(0, 0, 200, 100);
			Rect mr1 = new Rect(0, 0, 200, 100);
			Rect mr2 = new Rect(0, 0, 200, 100);
			Rect mr3 = new Rect(0, 0, 200, 100);
			Rect ar0 = new Rect(0, 0, 200, 100);
			Rect ar1 = new Rect(0, 0, 100, 100);
			Rect ar2 = new Rect(100, 0, 100, 100);
			Rect ar3 = new Rect(50, 0, 100, 100);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 200.0f, 100.0f);
			RPNode node1 = AddNodeToGraph(graph, e1, 100.0f, 100.0f);
			RPNode node2 = AddNodeToGraph(graph, e2, 100.0f, 100.0f);
			RPNode node3 = AddNodeToGraph(graph, e3, 100.0f, 100.0f);

			node1.SetAlignLeftWithPanelConstraint(true);
			node2.SetAlignRightWithPanelConstraint(true);
			node3.SetAlignHorizontalCenterWithPanelConstraint(true);

			graph.MeasureNodes(availableSize);
			panelSize = graph.CalculateDesiredSize();
			graph.ArrangeNodes(finalRect);

			VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
			VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
			VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
			VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
			VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
			VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
			VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
			VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
		}

		[TestMethod]
		public void VerifyAboveAndOutOfPanel()
		{
			Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			Rect finalRect = new Rect(0, 0, 100, 100);
			Rect mr0 = new Rect(0, 0, 100, 100);
			Rect mr1 = new Rect(0, 0, 100, 0);
			Rect ar0 = new Rect(0, 0, 100, 100);
			Rect ar1 = new Rect(0, 0, 100, 0);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);

			node0.SetAlignTopWithPanelConstraint(true);
			node1.SetAboveConstraint(node0);

			graph.MeasureNodes(availableSize);
			panelSize = graph.CalculateDesiredSize();
			graph.ArrangeNodes(finalRect);

			VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
			VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
			VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
			VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
			VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
			VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
		}

		[TestMethod]
		public void VerifyBelowAndOutOfPanel()
		{
			Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			Rect finalRect = new Rect(0, 0, 100, 100);
			Rect mr0 = new Rect(0, 0, 100, 100);
			Rect mr1 = new Rect(0, 100, 100, 0);
			Rect ar0 = new Rect(0, 0, 100, 100);
			Rect ar1 = new Rect(0, 100, 100, 0);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);

			node0.SetAlignBottomWithPanelConstraint(true);
			node1.SetBelowConstraint(node0);

			graph.MeasureNodes(availableSize);
			panelSize = graph.CalculateDesiredSize();
			graph.ArrangeNodes(finalRect);

			VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
			VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
			VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
			VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
			VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
			VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
		}

		[TestMethod]
		public void VerifyLeftAndOutOfPanel()
		{
			Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			Rect finalRect = new Rect(0, 0, 100, 100);
			Rect mr0 = new Rect(0, 0, 100, 100);
			Rect mr1 = new Rect(0, 0, 0, 100);
			Rect ar0 = new Rect(0, 0, 100, 100);
			Rect ar1 = new Rect(0, 0, 0, 100);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);

			node0.SetAlignLeftWithPanelConstraint(true);
			node1.SetLeftOfConstraint(node0);

			graph.MeasureNodes(availableSize);
			panelSize = graph.CalculateDesiredSize();
			graph.ArrangeNodes(finalRect);

			VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
			VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
			VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
			VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
			VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
			VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
		}

		[TestMethod]
		public void VerifyRightAndOutOfPanel()
		{
			Size availableSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			Rect finalRect = new Rect(0, 0, 100, 100);
			Rect mr0 = new Rect(0, 0, 100, 100);
			Rect mr1 = new Rect(100, 0, 0, 100);
			Rect ar0 = new Rect(0, 0, 100, 100);
			Rect ar1 = new Rect(100, 0, 0, 100);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);

			node0.SetAlignRightWithPanelConstraint(true);
			node1.SetRightOfConstraint(node0);

			graph.MeasureNodes(availableSize);
			panelSize = graph.CalculateDesiredSize();
			graph.ArrangeNodes(finalRect);

			VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
			VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
			VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
			VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
			VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
			VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
		}

		[TestMethod]
		public void VerifyVerticalMultiDirectionalLeaf()
		{
			Size availableSize = new Size(2000, 2000);

			{
				Rect finalRect = new Rect(0, 0, 100, 550);
				Rect mr0 = new Rect(0, 0, 100, 550);
				Rect mr1 = new Rect(0, 0, 100, 550);
				Rect mr2 = new Rect(0, 100, 100, 250);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 350, 100, 200);
				Rect ar2 = new Rect(0, 100, 100, 250);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 200);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 250);

				node0.SetAlignTopWithPanelConstraint(true);
				node1.SetAlignBottomWithPanelConstraint(true);
				node2.SetBelowConstraint(node0);
				node2.SetAboveConstraint(node1);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 450);
				Rect mr0 = new Rect(0, 0, 100, 450);
				Rect mr1 = new Rect(0, 0, 100, 450);
				Rect mr2 = new Rect(0, 100, 100, 150);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 250, 100, 200);
				Rect ar2 = new Rect(0, 100, 100, 150);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 200);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 150);

				node0.SetAlignTopWithPanelConstraint(true);
				node1.SetAlignBottomWithPanelConstraint(true);
				node2.SetBelowConstraint(node0);
				node2.SetAboveConstraint(node1);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 350);
				Rect mr0 = new Rect(0, 0, 100, 350);
				Rect mr1 = new Rect(0, 0, 100, 350);
				Rect mr2 = new Rect(0, 100, 100, 50);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 150, 100, 200);
				Rect ar2 = new Rect(0, 100, 100, 50);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 200);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 50);

				node0.SetAlignTopWithPanelConstraint(true);
				node1.SetAlignBottomWithPanelConstraint(true);
				node2.SetBelowConstraint(node0);
				node2.SetAboveConstraint(node1);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}
		}

		[TestMethod]
		public void VerifyHorizontalMultiDirectionalLeaf()
		{
			Size availableSize = new Size(2000, 2000);

			{
				Rect finalRect = new Rect(0, 0, 550, 100);
				Rect mr0 = new Rect(0, 0, 550, 100);
				Rect mr1 = new Rect(0, 0, 550, 100);
				Rect mr2 = new Rect(100, 0, 250, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(350, 0, 200, 100);
				Rect ar2 = new Rect(100, 0, 250, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 200, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 250, 100);

				node0.SetAlignLeftWithPanelConstraint(true);
				node1.SetAlignRightWithPanelConstraint(true);
				node2.SetRightOfConstraint(node0);
				node2.SetLeftOfConstraint(node1);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 450, 100);
				Rect mr0 = new Rect(0, 0, 450, 100);
				Rect mr1 = new Rect(0, 0, 450, 100);
				Rect mr2 = new Rect(100, 0, 150, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(250, 0, 200, 100);
				Rect ar2 = new Rect(100, 0, 150, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 200, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 150, 100);

				node0.SetAlignLeftWithPanelConstraint(true);
				node1.SetAlignRightWithPanelConstraint(true);
				node2.SetRightOfConstraint(node0);
				node2.SetLeftOfConstraint(node1);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 350, 100);
				Rect mr0 = new Rect(0, 0, 350, 100);
				Rect mr1 = new Rect(0, 0, 350, 100);
				Rect mr2 = new Rect(100, 0, 50, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(150, 0, 200, 100);
				Rect ar2 = new Rect(100, 0, 50, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 200, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 50, 100);

				node0.SetAlignLeftWithPanelConstraint(true);
				node1.SetAlignRightWithPanelConstraint(true);
				node2.SetRightOfConstraint(node0);
				node2.SetLeftOfConstraint(node1);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}
		}

		[TestMethod]
		public void VerifyAboveVerticallyCenteredRoot()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 200);

			node0.SetAlignVerticalCenterWithPanelConstraint(true);
			node1.SetAlignBottomWithConstraint(node0);

			graph.MeasureNodes(availableSize);

			{
				Rect finalRect = new Rect(0, 0, 100, 300);
				Rect mr0 = new Rect(0, 0, 100, 300);
				Rect mr1 = new Rect(0, 0, 100, 200);
				Rect ar0 = new Rect(0, 100, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 200);

				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 500);
				Rect mr0 = new Rect(0, 0, 100, 500);
				Rect mr1 = new Rect(0, 0, 100, 200);
				Rect ar0 = new Rect(0, 200, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 200);

				node1.SetAlignBottomWithConstraint(null);
				node1.SetAboveConstraint(node0);

				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			}
		}

		[TestMethod]
		public void VerifyBelowVerticallyCenteredRoot()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 200);

			node0.SetAlignVerticalCenterWithPanelConstraint(true);
			node1.SetAlignTopWithConstraint(node0);

			graph.MeasureNodes(availableSize);

			{
				Rect finalRect = new Rect(0, 0, 100, 300);
				Rect mr0 = new Rect(0, 0, 100, 300);
				Rect mr1 = new Rect(0, 100, 100, 200);
				Rect ar0 = new Rect(0, 100, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 200);

				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			}

			{
				Rect finalRect = new Rect(0, 0, 100, 500);
				Rect mr0 = new Rect(0, 0, 100, 500);
				Rect mr1 = new Rect(0, 300, 100, 200);
				Rect ar0 = new Rect(0, 200, 100, 100);
				Rect ar1 = new Rect(0, 300, 100, 200);

				node1.SetAlignTopWithConstraint(null);
				node1.SetBelowConstraint(node0);

				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			}
		}

		[TestMethod]
		public void VerifyLeftOfHorizontallyCenteredRoot()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 200, 100);

			node0.SetAlignHorizontalCenterWithPanelConstraint(true);
			node1.SetAlignRightWithConstraint(node0);

			graph.MeasureNodes(availableSize);

			{
				Rect finalRect = new Rect(0, 0, 300, 100);
				Rect mr0 = new Rect(0, 0, 300, 100);
				Rect mr1 = new Rect(0, 0, 200, 100);
				Rect ar0 = new Rect(100, 0, 100, 100);
				Rect ar1 = new Rect(0, 0, 200, 100);

				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			}

			{
				Rect finalRect = new Rect(0, 0, 500, 100);
				Rect mr0 = new Rect(0, 0, 500, 100);
				Rect mr1 = new Rect(0, 0, 200, 100);
				Rect ar0 = new Rect(200, 0, 100, 100);
				Rect ar1 = new Rect(0, 0, 200, 100);

				node1.SetAlignRightWithConstraint(null);
				node1.SetLeftOfConstraint(node0);

				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			}
		}

		[TestMethod]
		public void VerifyRightOfHorizontallyCenteredRoot()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 200, 100);

			node0.SetAlignHorizontalCenterWithPanelConstraint(true);
			node1.SetAlignLeftWithConstraint(node0);

			graph.MeasureNodes(availableSize);

			{
				Rect finalRect = new Rect(0, 0, 300, 100);
				Rect mr0 = new Rect(0, 0, 300, 100);
				Rect mr1 = new Rect(100, 0, 200, 100);
				Rect ar0 = new Rect(100, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 200, 100);

				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			}

			{
				Rect finalRect = new Rect(0, 0, 500, 100);
				Rect mr0 = new Rect(0, 0, 500, 100);
				Rect mr1 = new Rect(300, 0, 200, 100);
				Rect ar0 = new Rect(200, 0, 100, 100);
				Rect ar1 = new Rect(300, 0, 200, 100);

				node1.SetAlignLeftWithConstraint(null);
				node1.SetRightOfConstraint(node0);

				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
			}
		}

		[TestMethod]
		public void VerifyVerticallyCenteredChains()
		{
			Size availableSize = new Size(2000, 2000);
			{
				Rect finalRect = new Rect(0, 0, 200, 300);
				Rect mr0 = new Rect(0, 0, 200, 300);
				Rect mr1 = new Rect(0, 0, 200, 100);
				Rect mr2 = new Rect(100, 0, 100, 300);
				Rect ar0 = new Rect(0, 100, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 100);
				Rect ar2 = new Rect(100, 0, 100, 300);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 300);

				node0.SetAlignVerticalCenterWithPanelConstraint(true);
				node1.SetAboveConstraint(node0);
				node2.SetAlignTopWithConstraint(node1);
				node2.SetRightOfConstraint(node1);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 200, 300);
				Rect mr0 = new Rect(0, 0, 200, 300);
				Rect mr1 = new Rect(0, 200, 200, 100);
				Rect mr2 = new Rect(100, 0, 100, 300);
				Rect ar0 = new Rect(0, 100, 100, 100);
				Rect ar1 = new Rect(0, 200, 100, 100);
				Rect ar2 = new Rect(100, 0, 100, 300);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 300);

				node0.SetAlignVerticalCenterWithPanelConstraint(true);
				node1.SetBelowConstraint(node0);
				node2.SetAlignBottomWithConstraint(node1);
				node2.SetRightOfConstraint(node1);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}
		}

		[TestMethod]
		public void VerifyHorizontallyCenteredChains()
		{
			Size availableSize = new Size(2000, 2000);
			{
				Rect finalRect = new Rect(0, 0, 300, 200);
				Rect mr0 = new Rect(0, 0, 300, 200);
				Rect mr1 = new Rect(0, 0, 100, 200);
				Rect mr2 = new Rect(0, 100, 300, 100);
				Rect ar0 = new Rect(100, 0, 100, 100);
				Rect ar1 = new Rect(0, 0, 100, 100);
				Rect ar2 = new Rect(0, 100, 300, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 300, 100);

				node0.SetAlignHorizontalCenterWithPanelConstraint(true);
				node1.SetLeftOfConstraint(node0);
				node2.SetAlignLeftWithConstraint(node1);
				node2.SetBelowConstraint(node1);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}

			{
				Rect finalRect = new Rect(0, 0, 300, 200);
				Rect mr0 = new Rect(0, 0, 300, 200);
				Rect mr1 = new Rect(200, 0, 100, 200);
				Rect mr2 = new Rect(0, 100, 300, 100);
				Rect ar0 = new Rect(100, 0, 100, 100);
				Rect ar1 = new Rect(200, 0, 100, 100);
				Rect ar2 = new Rect(0, 100, 300, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 300, 100);

				node0.SetAlignHorizontalCenterWithPanelConstraint(true);
				node1.SetRightOfConstraint(node0);
				node2.SetAlignRightWithConstraint(node1);
				node2.SetBelowConstraint(node1);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
			}
		}

		[TestMethod]
		public void VerifySimultaneousTopAndBottomAlignment()
		{
			Size availableSize = new Size(2000, 2000);

			{
				Rect finalRect = new Rect(0, 0, 200, 400);
				Rect mr0 = new Rect(0, 0, 200, 400);
				Rect mr1 = new Rect(0, 100, 200, 300);
				Rect mr2 = new Rect(100, 100, 100, 200);
				Rect mr3 = new Rect(100, 300, 100, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 200);
				Rect ar2 = new Rect(100, 100, 100, 200);
				Rect ar3 = new Rect(100, 300, 100, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 200);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 100);
				RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);

				node0.SetAlignTopWithPanelConstraint(true);
				node1.SetBelowConstraint(node0);
				node2.SetRightOfConstraint(node1);
				node2.SetAlignTopWithConstraint(node1);
				node2.SetAlignBottomWithConstraint(node1);
				node3.SetBelowConstraint(node2);
				node3.SetAlignLeftWithConstraint(node2);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
			}

			{
				Rect finalRect = new Rect(0, 0, 200, 400);
				Rect mr0 = new Rect(0, 0, 200, 400);
				Rect mr1 = new Rect(0, 0, 200, 300);
				Rect mr2 = new Rect(100, 100, 100, 200);
				Rect mr3 = new Rect(100, 0, 100, 100);
				Rect ar0 = new Rect(0, 300, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 200);
				Rect ar2 = new Rect(100, 100, 100, 200);
				Rect ar3 = new Rect(100, 0, 100, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 200);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 100);
				RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);

				node0.SetAlignBottomWithPanelConstraint(true);
				node1.SetAboveConstraint(node0);
				node2.SetRightOfConstraint(node1);
				node2.SetAlignTopWithConstraint(node1);
				node2.SetAlignBottomWithConstraint(node1);
				node3.SetAboveConstraint(node2);
				node3.SetAlignLeftWithConstraint(node2);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
			}

			{
				Rect finalRect = new Rect(0, 0, 200, 400);
				Rect mr0 = new Rect(0, 0, 200, 400);
				Rect mr1 = new Rect(0, 100, 200, 300);
				Rect mr2 = new Rect(0, 200, 200, 200);
				Rect mr3 = new Rect(100, 100, 100, 200);
				Rect mr4 = new Rect(100, 300, 100, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 100);
				Rect ar2 = new Rect(0, 200, 100, 100);
				Rect ar3 = new Rect(100, 100, 100, 200);
				Rect ar4 = new Rect(100, 300, 100, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement(), e4 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 100);
				RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);
				RPNode node4 = AddNodeToGraph(graph, e4, 100, 100);

				node0.SetAlignTopWithPanelConstraint(true);
				node1.SetBelowConstraint(node0);
				node2.SetBelowConstraint(node1);
				node3.SetRightOfConstraint(node1);
				node3.SetAlignTopWithConstraint(node1);
				node3.SetAlignBottomWithConstraint(node2);
				node4.SetBelowConstraint(node3);
				node4.SetAlignLeftWithConstraint(node3);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
				VERIFY_ARE_EQUAL(node4.m_measureRect, mr4);
				VERIFY_ARE_EQUAL(node4.m_arrangeRect, ar4);
			}
		}

		[TestMethod]
		public void VerifySimultaneousLeftAndRightAlignment()
		{
			Size availableSize = new Size(2000, 2000);
			{
				Rect finalRect = new Rect(0, 0, 400, 200);
				Rect mr0 = new Rect(0, 0, 400, 200);
				Rect mr1 = new Rect(100, 0, 300, 200);
				Rect mr2 = new Rect(100, 100, 200, 100);
				Rect mr3 = new Rect(300, 100, 100, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 200, 100);
				Rect ar2 = new Rect(100, 100, 200, 100);
				Rect ar3 = new Rect(300, 100, 100, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 200, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 100);
				RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);

				node0.SetAlignLeftWithPanelConstraint(true);
				node1.SetRightOfConstraint(node0);
				node2.SetBelowConstraint(node1);
				node2.SetAlignLeftWithConstraint(node1);
				node2.SetAlignRightWithConstraint(node1);
				node3.SetRightOfConstraint(node2);
				node3.SetAlignTopWithConstraint(node2);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
			}

			{
				Rect finalRect = new Rect(0, 0, 400, 200);
				Rect mr0 = new Rect(0, 0, 400, 200);
				Rect mr1 = new Rect(0, 0, 300, 200);
				Rect mr2 = new Rect(100, 100, 200, 100);
				Rect mr3 = new Rect(0, 100, 100, 100);
				Rect ar0 = new Rect(300, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 200, 100);
				Rect ar2 = new Rect(100, 100, 200, 100);
				Rect ar3 = new Rect(0, 100, 100, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 200, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 100);
				RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);

				node0.SetAlignRightWithPanelConstraint(true);
				node1.SetLeftOfConstraint(node0);
				node2.SetBelowConstraint(node1);
				node2.SetAlignLeftWithConstraint(node1);
				node2.SetAlignRightWithConstraint(node1);
				node3.SetLeftOfConstraint(node2);
				node3.SetAlignTopWithConstraint(node2);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
			}

			{
				Rect finalRect = new Rect(0, 0, 400, 200);
				Rect mr0 = new Rect(0, 0, 400, 200);
				Rect mr1 = new Rect(100, 0, 300, 200);
				Rect mr2 = new Rect(200, 0, 200, 200);
				Rect mr3 = new Rect(100, 100, 200, 100);
				Rect mr4 = new Rect(300, 100, 100, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 100, 100);
				Rect ar2 = new Rect(200, 0, 100, 100);
				Rect ar3 = new Rect(100, 100, 200, 100);
				Rect ar4 = new Rect(300, 100, 100, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement(), e4 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 100);
				RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);
				RPNode node4 = AddNodeToGraph(graph, e4, 100, 100);

				node0.SetAlignLeftWithPanelConstraint(true);
				node1.SetRightOfConstraint(node0);
				node2.SetRightOfConstraint(node1);
				node3.SetBelowConstraint(node1);
				node3.SetAlignLeftWithConstraint(node1);
				node3.SetAlignRightWithConstraint(node2);
				node4.SetRightOfConstraint(node3);
				node4.SetAlignTopWithConstraint(node3);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
				VERIFY_ARE_EQUAL(node4.m_measureRect, mr4);
				VERIFY_ARE_EQUAL(node4.m_arrangeRect, ar4);
			}
		}

		[TestMethod]
		public void VerifySimultaneousTopAndBottomAlignmentToChains()
		{
			Size availableSize = new Size(2000, 2000);

			{
				Rect finalRect = new Rect(0, 0, 300, 600);
				Rect mr0 = new Rect(0, 0, 300, 600);
				Rect mr1 = new Rect(0, 100, 300, 500);
				Rect mr2 = new Rect(0, 0, 300, 600);
				Rect mr3 = new Rect(0, 0, 300, 500);
				Rect mr4 = new Rect(100, 100, 200, 200);
				Rect mr5 = new Rect(100, 0, 200, 500);
				Rect mr6 = new Rect(200, 100, 100, 400);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 100);
				Rect ar2 = new Rect(0, 500, 100, 100);
				Rect ar3 = new Rect(0, 400, 100, 100);
				Rect ar4 = new Rect(100, 100, 100, 200);
				Rect ar5 = new Rect(100, 300, 100, 200);
				Rect ar6 = new Rect(200, 100, 100, 400);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement(), e4 = new UIElement(), e5 = new UIElement(), e6 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 100);
				RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);
				RPNode node4 = AddNodeToGraph(graph, e4, 100, 200);
				RPNode node5 = AddNodeToGraph(graph, e5, 100, 200);
				RPNode node6 = AddNodeToGraph(graph, e6, 100, 400);

				node0.SetAlignTopWithPanelConstraint(true);
				node1.SetBelowConstraint(node0);
				node2.SetAlignBottomWithPanelConstraint(true);
				node3.SetAboveConstraint(node2);
				node4.SetAlignTopWithConstraint(node1);
				node4.SetRightOfConstraint(node1);
				node4.SetAboveConstraint(node5);
				node5.SetRightOfConstraint(node3);
				node5.SetAlignBottomWithConstraint(node3);
				node6.SetAlignTopWithConstraint(node4);
				node6.SetAlignBottomWithConstraint(node5);
				node6.SetRightOfConstraint(node4);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
				VERIFY_ARE_EQUAL(node4.m_measureRect, mr4);
				VERIFY_ARE_EQUAL(node4.m_arrangeRect, ar4);
				VERIFY_ARE_EQUAL(node5.m_measureRect, mr5);
				VERIFY_ARE_EQUAL(node5.m_arrangeRect, ar5);
				VERIFY_ARE_EQUAL(node6.m_measureRect, mr6);
				VERIFY_ARE_EQUAL(node6.m_arrangeRect, ar6);
			}

			{
				Rect finalRect = new Rect(0, 0, 300, 600);
				Rect mr0 = new Rect(0, 0, 300, 600);
				Rect mr1 = new Rect(0, 100, 300, 500);
				Rect mr2 = new Rect(0, 0, 300, 600);
				Rect mr3 = new Rect(0, 0, 300, 500);
				Rect mr4 = new Rect(100, 100, 200, 400);
				Rect mr5 = new Rect(200, 100, 100, 400);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(0, 100, 100, 100);
				Rect ar2 = new Rect(0, 500, 100, 100);
				Rect ar3 = new Rect(0, 400, 100, 100);
				Rect ar4 = new Rect(100, 100, 100, 400);
				Rect ar5 = new Rect(200, 100, 100, 400);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement(), e4 = new UIElement(), e5 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 100);
				RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);
				RPNode node4 = AddNodeToGraph(graph, e4, 100, 400);
				RPNode node5 = AddNodeToGraph(graph, e5, 100, 400);

				node0.SetAlignTopWithPanelConstraint(true);
				node1.SetBelowConstraint(node0);
				node2.SetAlignBottomWithPanelConstraint(true);
				node3.SetAboveConstraint(node2);
				node4.SetAlignTopWithConstraint(node1);
				node4.SetAlignBottomWithConstraint(node3);
				node4.SetRightOfConstraint(node1);
				node5.SetAlignTopWithConstraint(node4);
				node5.SetAlignBottomWithConstraint(node4);
				node5.SetRightOfConstraint(node4);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
				VERIFY_ARE_EQUAL(node4.m_measureRect, mr4);
				VERIFY_ARE_EQUAL(node4.m_arrangeRect, ar4);
				VERIFY_ARE_EQUAL(node5.m_measureRect, mr5);
				VERIFY_ARE_EQUAL(node5.m_arrangeRect, ar5);
			}
		}

		[TestMethod]
		public void VerifySimultaneousLeftAndRightAlignmentToChains()
		{
			Size availableSize = new Size(2000, 2000);

			{
				Rect finalRect = new Rect(0, 0, 600, 300);
				Rect mr0 = new Rect(0, 0, 600, 300);
				Rect mr1 = new Rect(100, 0, 500, 300);
				Rect mr2 = new Rect(0, 0, 600, 300);
				Rect mr3 = new Rect(0, 0, 500, 300);
				Rect mr4 = new Rect(100, 100, 200, 200);
				Rect mr5 = new Rect(0, 100, 500, 200);
				Rect mr6 = new Rect(100, 200, 400, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 100, 100);
				Rect ar2 = new Rect(500, 0, 100, 100);
				Rect ar3 = new Rect(400, 0, 100, 100);
				Rect ar4 = new Rect(100, 100, 200, 100);
				Rect ar5 = new Rect(300, 100, 200, 100);
				Rect ar6 = new Rect(100, 200, 400, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement(), e4 = new UIElement(), e5 = new UIElement(), e6 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 100);
				RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);
				RPNode node4 = AddNodeToGraph(graph, e4, 200, 100);
				RPNode node5 = AddNodeToGraph(graph, e5, 200, 100);
				RPNode node6 = AddNodeToGraph(graph, e6, 400, 100);

				node0.SetAlignLeftWithPanelConstraint(true);
				node1.SetRightOfConstraint(node0);
				node2.SetAlignRightWithPanelConstraint(true);
				node3.SetLeftOfConstraint(node2);
				node4.SetAlignLeftWithConstraint(node1);
				node4.SetBelowConstraint(node1);
				node4.SetLeftOfConstraint(node5);
				node5.SetBelowConstraint(node3);
				node5.SetAlignRightWithConstraint(node3);
				node6.SetAlignLeftWithConstraint(node4);
				node6.SetAlignRightWithConstraint(node5);
				node6.SetBelowConstraint(node4);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
				VERIFY_ARE_EQUAL(node4.m_measureRect, mr4);
				VERIFY_ARE_EQUAL(node4.m_arrangeRect, ar4);
				VERIFY_ARE_EQUAL(node5.m_measureRect, mr5);
				VERIFY_ARE_EQUAL(node5.m_arrangeRect, ar5);
				VERIFY_ARE_EQUAL(node6.m_measureRect, mr6);
				VERIFY_ARE_EQUAL(node6.m_arrangeRect, ar6);
			}

			{
				Rect finalRect = new Rect(0, 0, 600, 300);
				Rect mr0 = new Rect(0, 0, 600, 300);
				Rect mr1 = new Rect(100, 0, 500, 300);
				Rect mr2 = new Rect(0, 0, 600, 300);
				Rect mr3 = new Rect(0, 0, 500, 300);
				Rect mr4 = new Rect(100, 100, 400, 200);
				Rect mr5 = new Rect(100, 200, 400, 100);
				Rect ar0 = new Rect(0, 0, 100, 100);
				Rect ar1 = new Rect(100, 0, 100, 100);
				Rect ar2 = new Rect(500, 0, 100, 100);
				Rect ar3 = new Rect(400, 0, 100, 100);
				Rect ar4 = new Rect(100, 100, 400, 100);
				Rect ar5 = new Rect(100, 200, 400, 100);

				RPGraph graph = new RPGraph();
				UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement(), e4 = new UIElement(), e5 = new UIElement();
				Size panelSize = new Size();

				RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
				RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);
				RPNode node2 = AddNodeToGraph(graph, e2, 100, 100);
				RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);
				RPNode node4 = AddNodeToGraph(graph, e4, 400, 100);
				RPNode node5 = AddNodeToGraph(graph, e5, 400, 100);

				node0.SetAlignLeftWithPanelConstraint(true);
				node1.SetRightOfConstraint(node0);
				node2.SetAlignRightWithPanelConstraint(true);
				node3.SetLeftOfConstraint(node2);
				node4.SetAlignLeftWithConstraint(node1);
				node4.SetAlignRightWithConstraint(node3);
				node4.SetBelowConstraint(node1);
				node5.SetAlignLeftWithConstraint(node4);
				node5.SetAlignRightWithConstraint(node4);
				node5.SetBelowConstraint(node4);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
				VERIFY_ARE_EQUAL(node4.m_measureRect, mr4);
				VERIFY_ARE_EQUAL(node4.m_arrangeRect, ar4);
				VERIFY_ARE_EQUAL(node5.m_measureRect, mr5);
				VERIFY_ARE_EQUAL(node5.m_arrangeRect, ar5);
			}
		}

		[TestMethod]
		public void VerifyChainWithHorizontallyCenteredSiblings()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 200, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 200, 100);
			RPNode node2 = AddNodeToGraph(graph, e2, 300, 100);
			RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);

			node0.SetAlignTopWithPanelConstraint(true);
			node0.SetAlignLeftWithPanelConstraint(true);
			node1.SetRightOfConstraint(node0);
			node2.SetBelowConstraint(node1);
			node2.SetAlignHorizontalCenterWithConstraint(node1);
			node3.SetLeftOfConstraint(node2);
			node3.SetAlignTopWithConstraint(node2);

			{
				Rect finalRect = new Rect(0, 0, 450, 200);
				Rect mr0 = new Rect(0, 0, 450, 200);
				Rect mr1 = new Rect(200, 0, 250, 200);
				Rect mr2 = new Rect(150, 100, 300, 100);
				Rect mr3 = new Rect(0, 100, 150, 100);
				Rect ar0 = new Rect(0, 0, 200, 100);
				Rect ar1 = new Rect(200, 0, 200, 100);
				Rect ar2 = new Rect(150, 100, 300, 100);
				Rect ar3 = new Rect(50, 100, 100, 100);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
			}

			{
				Rect finalRect = new Rect(0, 0, 550, 200);
				Rect mr0 = new Rect(0, 0, 550, 200);
				Rect mr1 = new Rect(200, 0, 350, 200);
				Rect mr2 = new Rect(50, 100, 500, 100);
				Rect mr3 = new Rect(450, 100, 100, 100);
				Rect ar0 = new Rect(0, 0, 200, 100);
				Rect ar1 = new Rect(200, 0, 200, 100);
				Rect ar2 = new Rect(150, 100, 300, 100);
				Rect ar3 = new Rect(450, 100, 100, 100);

				node3.SetLeftOfConstraint(null);
				node3.SetRightOfConstraint(node2);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
			}
		}

		[TestMethod]
		public void VerifyChainWithVerticallyCenteredSiblings()
		{
			Size availableSize = new Size(2000, 2000);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement(), e2 = new UIElement(), e3 = new UIElement();
			Size panelSize = new Size();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 200);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 200);
			RPNode node2 = AddNodeToGraph(graph, e2, 100, 300);
			RPNode node3 = AddNodeToGraph(graph, e3, 100, 100);

			node0.SetAlignTopWithPanelConstraint(true);
			node0.SetAlignLeftWithPanelConstraint(true);
			node1.SetBelowConstraint(node0);
			node2.SetRightOfConstraint(node1);
			node2.SetAlignVerticalCenterWithConstraint(node1);
			node3.SetAboveConstraint(node2);
			node3.SetAlignLeftWithConstraint(node2);

			{
				Rect finalRect = new Rect(0, 0, 200, 450);
				Rect mr0 = new Rect(0, 0, 200, 450);
				Rect mr1 = new Rect(0, 200, 200, 250);
				Rect mr2 = new Rect(100, 150, 100, 300);
				Rect mr3 = new Rect(100, 0, 100, 150);
				Rect ar0 = new Rect(0, 0, 100, 200);
				Rect ar1 = new Rect(0, 200, 100, 200);
				Rect ar2 = new Rect(100, 150, 100, 300);
				Rect ar3 = new Rect(100, 50, 100, 100);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
			}

			{
				Rect finalRect = new Rect(0, 0, 200, 550);
				Rect mr0 = new Rect(0, 0, 200, 550);
				Rect mr1 = new Rect(0, 200, 200, 350);
				Rect mr2 = new Rect(100, 50, 100, 500);
				Rect mr3 = new Rect(100, 450, 100, 100);
				Rect ar0 = new Rect(0, 0, 100, 200);
				Rect ar1 = new Rect(0, 200, 100, 200);
				Rect ar2 = new Rect(100, 150, 100, 300);
				Rect ar3 = new Rect(100, 450, 100, 100);

				node3.SetAboveConstraint(null);
				node3.SetBelowConstraint(node2);

				graph.MeasureNodes(availableSize);
				panelSize = graph.CalculateDesiredSize();
				graph.ArrangeNodes(finalRect);

				VERIFY_ARE_EQUAL(panelSize.Width, finalRect.Width);
				VERIFY_ARE_EQUAL(panelSize.Height, finalRect.Height);
				VERIFY_ARE_EQUAL(node0.m_measureRect, mr0);
				VERIFY_ARE_EQUAL(node0.m_arrangeRect, ar0);
				VERIFY_ARE_EQUAL(node1.m_measureRect, mr1);
				VERIFY_ARE_EQUAL(node1.m_arrangeRect, ar1);
				VERIFY_ARE_EQUAL(node2.m_measureRect, mr2);
				VERIFY_ARE_EQUAL(node2.m_arrangeRect, ar2);
				VERIFY_ARE_EQUAL(node3.m_measureRect, mr3);
				VERIFY_ARE_EQUAL(node3.m_arrangeRect, ar3);
			}
		}

		[TestMethod]
		public void VerifyPhysicallyImpossibleDefinitions()
		{
			Size availableSize = new Size(2000, 2000);
			Rect finalRect = new Rect(0, 0, 200, 100);

			RPGraph graph = new RPGraph();
			UIElement e0 = new UIElement(), e1 = new UIElement();

			RPNode node0 = AddNodeToGraph(graph, e0, 100, 100);
			RPNode node1 = AddNodeToGraph(graph, e1, 100, 100);

			node0.SetAlignTopWithPanelConstraint(true);
			node1.SetRightOfConstraint(node0);
			node1.SetLeftOfConstraint(node0);
			node1.SetAboveConstraint(node0);
			node1.SetBelowConstraint(node0);

			graph.MeasureNodes(availableSize);
			graph.ArrangeNodes(finalRect);

			VERIFY_IS_LESS_THAN(node1.m_measureRect.Width, 0);
			VERIFY_IS_LESS_THAN(node1.m_arrangeRect.Width, 0);
			VERIFY_IS_LESS_THAN(node1.m_measureRect.Height, 0);
			VERIFY_IS_LESS_THAN(node1.m_arrangeRect.Height, 0);
		}
	}
}
#endif
