#nullable enable

using System;
using System.Runtime.Serialization;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Defines a XAML parsing exception to be raised from the generator
	/// </summary>
	[Serializable]
	internal class XamlParsingException : Exception, IXamlLocation
	{
		public XamlParsingException(string message, Exception? innerException, int lineNumber, int linePosition, string filePath) : base(message, innerException)
		{
			LineNumber = lineNumber;
			LinePosition = linePosition;
			FilePath = filePath;
		}

		public int LineNumber { get; }
		public int LinePosition { get; }
		public string FilePath { get; }
	}
}
