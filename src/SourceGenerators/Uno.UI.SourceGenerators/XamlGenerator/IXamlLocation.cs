#nullable enable

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Used as simple way of identifying the source location of an element in XAML
	/// </summary>
	internal interface IXamlLocation
	{
		public int LineNumber { get; }

		public int LinePosition { get; }
	}

}
