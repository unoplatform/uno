using Uno.UI.Extensions;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Defines an area within which you can position and align child
/// objects in relation to each other or the parent panel.
/// </summary>
public partial class RelativePanel : Panel
{
	private readonly RPGraph m_graph = new RPGraph();

	/// <summary>
	/// Initializes a new instance of the RelativePanel class.
	/// </summary>
	public RelativePanel()
	{
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		Size availableSizeForChildren = new Size();
		Size borderSize = Border.HelperGetCombinedThickness(this);

		// The available size for the children is equal to the available
		// size for the panel minus the size of the border.
		availableSizeForChildren.Width = availableSize.Width - borderSize.Width;
		availableSizeForChildren.Height = availableSize.Height - borderSize.Height;

		GenerateGraph();
		m_graph.MeasureNodes(availableSizeForChildren);

		// Now that the children have been measured, we can calculate
		// the desired size of the panel, which corresponds to the 
		// desired size of the children as a whole plus the size of
		// the border.
		Size desiredSizeOfChildren = m_graph.CalculateDesiredSize();

		var desiredSize = new Size();
		desiredSize.Width = desiredSizeOfChildren.Width + borderSize.Width;
		desiredSize.Height = desiredSizeOfChildren.Height + borderSize.Height;
		return desiredSize;

		//// If this is a known error, we must throw the appropriate
		//// exception based on the AgCode set by the helper class.
		//// Otherwise we just fail normally.
		// TODO Uno specific: In our case we directly throw InvalidOperationException in the RPGraph code.
	}

	protected override Size ArrangeOverride(Size finalSize)
	{
		Rect childRect = Border.HelperGetInnerRect(this, finalSize);

		m_graph.ArrangeNodes(childRect);
		return finalSize;
	}

	private void GenerateGraph()
	{
		var children = Children;

		m_graph.GetNodes().Clear();

		if (children != null)
		{
			var core = this.GetContext();

			// Create a node for each child and add it to the graph.
			foreach (var child in children)
			{
				m_graph.GetNodes().AddLast(new RPNode(child));
			}

			//var namescopeInfo = core.GetAdjustedReferenceObjectAndNamescopeType(this);

			// Now that we have all the nodes, we can build an adjacency list
			// based on the dependencies that each child has on its siblings, 
			// if any.
			m_graph.ResolveConstraints(this, core);
		}
	}
}
