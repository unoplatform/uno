using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Composition;

internal enum CompositionPathCommandType
{
	SetFillMode,
	SetSegmentFlags,
	BeginFigure,
	AddLine,
	AddLines,
	AddBezier,
	AddBeziers,
	AddQuadraticBezier,
	AddQuadraticBeziers,
	AddArc,
	EndFigure,
	Close
}
