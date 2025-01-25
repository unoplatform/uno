#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Composition;

internal interface ICompositionPathCommandsProvider
{
	List<CompositionPathCommand> Commands { get; }
}
