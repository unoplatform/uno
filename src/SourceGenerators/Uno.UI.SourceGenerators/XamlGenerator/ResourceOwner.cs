namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Resource owner variable definition
	/// </summary>
	internal class ResourceOwner
	{
		static int _resourceOwners;

		public ResourceOwner()
		{
			Name = "__ResourceOwner_" + (_resourceOwners++).ToString();
		}

		/// <summary>
		/// Gets the name of the current resource owner variable
		/// </summary>
		public string Name { get; }
	}
}
