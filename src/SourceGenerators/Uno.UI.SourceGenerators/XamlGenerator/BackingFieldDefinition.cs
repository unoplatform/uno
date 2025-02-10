#nullable enable

using System;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal record BackingFieldDefinition(string GlobalizedTypeName, string Name, Accessibility Accessibility);
