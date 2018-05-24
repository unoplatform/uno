using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation.Collections;

namespace Windows.Storage
{
	public partial class ApplicationDataContainer
	{
		internal ApplicationDataContainer(string name, ApplicationDataLocality locality)
		{
			Locality = locality;
			Name = name;

			InitializePartial();
		}

		partial void InitializePartial();

		public ApplicationDataLocality Locality { get; }

		public string Name { get; }

		public IPropertySet Values
		{
			get; private set;
		}
	}
}
