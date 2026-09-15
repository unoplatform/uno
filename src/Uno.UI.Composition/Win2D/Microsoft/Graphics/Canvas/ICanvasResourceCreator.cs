using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Graphics.Canvas;

/// <summary>
/// Implemented by objects that can create graphics resources, exposing the device those resources
/// belong to.
/// </summary>
internal interface ICanvasResourceCreator
{
	/// <summary>
	/// Gets the device that resources created by this object belong to.
	/// </summary>
	CanvasDevice Device { get; }
}
