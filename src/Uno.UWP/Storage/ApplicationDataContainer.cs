#nullable disable

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;

namespace Windows.Storage
{
	public partial class ApplicationDataContainer
	{
		internal ApplicationDataContainer(ApplicationData owner, string name, ApplicationDataLocality locality)
		{
			Locality = locality;
			Name = name;

			InitializePartial(owner);
		}

		partial void InitializePartial(ApplicationData owner);

		public ApplicationDataLocality Locality { get; }

		public string Name { get; }

		public IPropertySet Values { get; private set; }
	}
}
