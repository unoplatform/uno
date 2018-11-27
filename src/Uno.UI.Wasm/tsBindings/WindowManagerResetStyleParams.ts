/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerResetStyleParams
{
	/* Pack=4 */
	HtmlId : number;
	Styles_Length : number;
	Styles : Array<string>;
	public static deserialize(pData:number) : WindowManagerResetStyleParams
	{
		let ret = new WindowManagerResetStyleParams();
		ret.HtmlId = Number((Module.getValue(pData + 0, "*")));
		ret.Styles_Length = Number((Module.getValue(pData + 4, "i32")));
		
		{
			ret.Styles = new Array<string>();
			var pArray = Module.getValue(pData + 8, "*");
			for(var i=0; i<ret.Styles_Length; i++)
			{
				ret.Styles.push(String(MonoRuntime.conv_string(Module.getValue(pArray + i*4, "*"))));
			}
		}
		return ret;
	}
}
