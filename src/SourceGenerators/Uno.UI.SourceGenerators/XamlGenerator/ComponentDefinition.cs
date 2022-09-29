namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Definition for Components used for lazy static resources and x:Load marked objects
	/// </summary>
	internal class ComponentDefinition
	{
		public ComponentDefinition(XamlObjectDefinition xamlObject, bool isWeakReference)
		{
			IsWeakReference = isWeakReference;
			XamlObject = xamlObject;
		}

		public bool IsWeakReference { get; }
		public XamlObjectDefinition XamlObject { get; }
	}
}
