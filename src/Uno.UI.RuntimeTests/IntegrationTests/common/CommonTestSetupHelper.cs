namespace Private.Infrastructure
{
	internal static class CommonTestSetupHelper
	{
		private static bool _isInitialized;

		internal static void CommonTestClassSetup()
		{
			if (!_isInitialized)
			{
				TestServices.EnsureInitialized();
			}
			_isInitialized = true;
		}
	}
}
