#if !NET461
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.DataTransfer
{
	public partial class DataPackageView
	{
		private List<string> _availableFormats = new List<string>();
		private Task<string> _textTask = null;

		public IReadOnlyList<string> AvailableFormats => _availableFormats.AsReadOnly();

		internal void SetTextTask(Task<string> textTask)
		{
			_textTask = textTask;
			if (_textTask != null)
			{
				_availableFormats.Add(StandardDataFormats.Text);
			}
			else
			{
				_availableFormats.Remove(StandardDataFormats.Text);
			}
		}

		public IAsyncOperation<string> GetTextAsync()
		{
			if (!_availableFormats.Contains(StandardDataFormats.Text))
			{
				throw new InvalidOperationException("DataPackage does not contain Text data.");
			}
			return _textTask.AsAsyncOperation();
		}

		public bool Contains(string formatId)
        {
            if (formatId is null)
            {
                throw new ArgumentNullException(nameof(formatId));
            }

            return _availableFormats.Contains(formatId);
        }
    }
}
#endif
