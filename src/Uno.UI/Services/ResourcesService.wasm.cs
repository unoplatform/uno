
namespace Uno.UI.Services
{
	public class ResourcesService : IResourcesService
	{
		public ResourcesService()
		{
		}


		public string Get(string id)
		{
			return "[" + id + "]";
		}
	}
}
