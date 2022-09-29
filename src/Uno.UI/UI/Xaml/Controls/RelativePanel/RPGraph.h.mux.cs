#nullable disable

using System.Collections.Generic;
using Windows.Foundation;

namespace Uno.UI.Xaml.Controls;

internal partial class RPGraph
{
	public RPGraph()
	{
	}

	public LinkedList<RPNode> GetNodes() => m_nodes;

	private LinkedList<RPNode> m_nodes = new LinkedList<RPNode>();

	private Size m_availableSizeForNodeResolution;

	private double m_minX;
	private double m_maxX;
	private double m_minY;
	private double m_maxY;
	private bool m_isMinCapped;
	private bool m_isMaxCapped;
}
