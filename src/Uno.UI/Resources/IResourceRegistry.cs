#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Uno.Presentation.Resources
{
	public interface IResourceRegistry
	{
		object FindResource(string name);
		object GetResource(string name);
	}
}
