using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.ApplicationModel.DataTransfer
{
	internal interface IDataTransferManagerExtension
    {
		bool IsSupported();

		Task<bool> ShowShareUIAsync(ShareUIOptions options, DataPackage dataPackage);
	}
}
