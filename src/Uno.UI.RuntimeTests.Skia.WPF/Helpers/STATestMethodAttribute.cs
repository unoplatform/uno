namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
	public class STATestMethodAttribute : TestMethodAttribute
	{
		public override TestResult[] Execute(ITestMethod testMethod)
		{
			if (System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.STA)
				return Invoke(testMethod);

			TestResult[] result = null;
			var thread = new System.Threading.Thread(() => result = Invoke(testMethod));
			thread.SetApartmentState(System.Threading.ApartmentState.STA);
			thread.Start();
			thread.Join();
			return result;
		}

		private TestResult[] Invoke(ITestMethod testMethod)
		{
			return new[] { testMethod.Invoke(null) };
		}
	}
}
