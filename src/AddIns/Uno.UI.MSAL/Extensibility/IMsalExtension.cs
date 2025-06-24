using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Uno.UI.MSAL.Extensibility;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMsalExtension
{
	T InitializeAbstractApplicationBuilder<T>(T builder) where T : AbstractApplicationBuilder<T>;

	AcquireTokenInteractiveParameterBuilder InitializeAcquireTokenInteractiveParameterBuilder(AcquireTokenInteractiveParameterBuilder builder);
}
