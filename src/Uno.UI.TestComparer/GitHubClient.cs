using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace Uno.UI.TestComparer
{
	internal class GitHubClient
	{
		public static async Task PostPRCommentsAsync(string githubPAT, string sourceRepository, string githubPRid, string comment)
		{
			var uri = new Uri(sourceRepository);
			var path = uri.GetComponents(UriComponents.Path, UriFormat.UriEscaped).Replace(".git", "");

			var bodyContent = $"{{ \"body\": \"{HttpUtility.JavaScriptStringEncode(comment)}\" }}";

			await PostDocument(new Uri($"https://api.github.com/repos/{path}/issues/{githubPRid}/comments"), githubPAT, bodyContent);
		}

		private static async Task<dynamic> PostDocument(Uri contentUri, string githubPAT, string document)
		{
			var wc = new System.Net.WebClient();
			wc.Headers.Add("User-agent", "uno-nv-sync");
			wc.Headers.Add("Authorization", $"token {githubPAT}");
			wc.Encoding = UTF8Encoding.UTF8;

			var result = await wc.UploadStringTaskAsync(contentUri, "POST", document);

			return JsonConvert.DeserializeObject(result);
		}
	}
}
