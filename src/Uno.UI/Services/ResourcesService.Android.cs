using System;

namespace Uno.UI.Services
{
	public class ResourcesService : IResourcesService
	{
		private Android.Content.Context _applicationContext;

		public ResourcesService(Android.Content.Context applicationContext)
		{
			this._applicationContext = applicationContext;
		}


		public string Get(string id)
		{
			// double reflection in GetIdentifier, should be replaced by something better
			var intId = _applicationContext.Resources.GetIdentifier(AndroidResourceNameEncoder.Encode(id), "string", _applicationContext.PackageName);

			if (intId != 0)
			{
				return _applicationContext.Resources.GetString(intId);
			}

			return "";
		}
	}
}
