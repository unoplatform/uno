#nullable enable

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Resource owner variable definition
	/// </summary>
	/// <remarks>
	/// The ResourceOwner scope is used to propagate the top-level owner of the resources
	/// in order for FrameworkTemplates contents to access the code-behind context, without
	/// causing circular references and cause memory leaks.
	/// </remarks>
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
