using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.System.UserProfile
{
	public partial class UserProfilePersonalizationSettings
	{
		private UserProfilePersonalizationSettings()
		{
		}

#if !__ANDROID__
		public static bool IsSupported() => false;
#endif
	}
}
