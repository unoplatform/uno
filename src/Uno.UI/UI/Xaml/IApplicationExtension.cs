namespace Uno.UI.Xaml
{
	internal interface IApplicationExtension
	{
		bool CanExit { get; }

		void Exit();
	}
}
