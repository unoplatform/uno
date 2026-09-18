using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.Services.Maps
{
	public enum MapLocationFinderStatus
	{
		Success = 0,
		UnknownError = 1,
		InvalidCredentials = 2,
		BadLocation = 3,
		IndexFailure = 4,
		NetworkFailure = 5,
		NotSupported = 6
	}
}
