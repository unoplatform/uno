#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal record GenerationRunFileInfo(GenerationRunInfo RunInfo, string FileId);
}
