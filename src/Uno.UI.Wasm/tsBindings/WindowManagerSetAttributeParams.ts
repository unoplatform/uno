/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetAttributeParams
{
	/* Pack=4 */
	HtmlId : number;
	Pairs_Length : number;
	Pairs : Array<string>;
	public static unmarshal(pData:number) : WindowManagerSetAttributeParams
	{
		let ret = new WindowManagerSetAttributeParams();
		ret.HtmlId = Number((Module.getValue(pData + 0, "*")));
		ret.Pairs_Length = Number((Module.getValue(pData + 4, "i32")));
		
		{
			ret.Pairs = new Array<string>();
			var pArray = Module.getValue(pData + 8, "*");
			for(var i=0; i<ret.Pairs_Length; i++)
			{
				ret.Pairs.push(String(MonoRuntime.conv_string(Module.getValue(pArray + i*4, "*"))));
			}
		}
		return ret;
	}
}
