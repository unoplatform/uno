using System.ComponentModel;

namespace Uno.UI;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IResourcesService
{
	string Get(string id);
}
