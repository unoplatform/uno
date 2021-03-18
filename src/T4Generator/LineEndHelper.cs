using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace T4Generator
{
	static class Tools
	{

		public static string ConvertLineEndings(string input)
		{

			// convert to common (Unix) line endings
			string output = input.Replace("\r\n", "\n");    // from Windows
			output = output.Replace("\r", "\n");            // from old Mac

			// if current line endings is different, make conversion
			if (Environment.NewLine != "\n")
			{
				output = output.Replace("\n", Environment.NewLine);
			}

			return output;

		}
	}

}
