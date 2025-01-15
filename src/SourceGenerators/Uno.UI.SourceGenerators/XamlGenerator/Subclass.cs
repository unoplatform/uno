#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal record Subclass(XamlMemberDefinition ContentOwner, string ReturnType, string DefaultBindMode);
}
