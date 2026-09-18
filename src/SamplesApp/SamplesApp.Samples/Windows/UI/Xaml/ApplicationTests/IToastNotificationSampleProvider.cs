using System.Threading.Tasks;

public interface IToastNotificationSampleProvider
{
	Task ShowToastAsync(string title, string content);
}
