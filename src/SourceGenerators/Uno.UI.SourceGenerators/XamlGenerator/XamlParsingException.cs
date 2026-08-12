#nullable enable

using System;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Defines a XAML parsing exception to be raised from the generator
	/// </summary>
	[Serializable]
	internal class XamlParsingException : Exception, IXamlLocation
	{
		public XamlParsingException(string message, Exception? innerException, int lineNumber, int linePosition, string filePath)
			: this(message, innerException, lineNumber, linePosition, filePath, descriptor: null)
		{
		}

		public XamlParsingException(string message, Exception? innerException, int lineNumber, int linePosition, string filePath, DiagnosticDescriptor? descriptor) : base(message, innerException)
		{
			LineNumber = lineNumber;
			LinePosition = linePosition;
			FilePath = filePath;
			Descriptor = descriptor;
		}

		public int LineNumber { get; }
		public int LinePosition { get; }
		public string FilePath { get; }

		/// <summary>
		/// The diagnostic to report this error under, when it deserves a dedicated code instead of the generic <c>UXAML0001</c>.
		/// </summary>
		public DiagnosticDescriptor? Descriptor { get; }
	}
}
