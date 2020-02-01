
namespace Uno.UI.Services
{
	public class ResourcesService : IResourcesService
	{
		public string Get(string id)
		{
			return "[" + id + "]";
		}
	}
}