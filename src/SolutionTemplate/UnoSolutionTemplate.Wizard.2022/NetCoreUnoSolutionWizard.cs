#nullable enable


namespace UnoSolutionTemplate.Wizard
{
	public class NetCoreUnoSolutionWizard : UnoSolutionWizard
	{
		public NetCoreUnoSolutionWizard() : base(enableNuGetConfig: true, vsSuffix: "vs17")
		{

		}
	}
}
