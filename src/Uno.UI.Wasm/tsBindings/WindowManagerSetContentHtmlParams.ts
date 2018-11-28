/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetContentHtmlParams
{
	/* Pack=4 */
	HtmlId : number;
	Html : string;
	public static unmarshal(pData:number) : WindowManagerSetContentHtmlParams
	{
		let ret = new WindowManagerSetContentHtmlParams();
		ret.HtmlId = Number((Module.getValue(pData + 0, "*")));
		ret.Html = String(Module.UTF8ToString(Module.getValue(pData + 4, "*")));
		return ret;
	}
}
