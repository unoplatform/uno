using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Content;

/// <summary>
/// Represents a hosting site for a ContentIsland.
/// </summary>
internal interface IContentIslandHost
{
	float RasterizationScale { get; }

	bool IsSiteVisible { get; }
}
