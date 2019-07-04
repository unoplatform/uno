/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetAttributesParams
{
	/* Pack=4 */
	HtmlId : number;
	Pairs_Length : number;
	Pairs : Array<string>;
	public static unmarshal(pData:number) : WindowManagerSetAttributesParams
	{
		let ret = new WindowManagerSetAttributesParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.Pairs_Length = Number(Module.getValue(pData + 4, "i32"));
		}
		
		{
			var pArray = Module.getValue(pData + 8, "*");
			if(pArray !== 0)
			{
				ret.Pairs = new Array<string>();
				for(var i=0; i<ret.Pairs_Length; i++)
				{
					var value = Module.getValue(pArray + i * 4, "*");
					if(value !== 0)
					{
						ret.Pairs.push(String(MonoRuntime.conv_string(value)));
					}
					else
					
					{
						ret.Pairs.push(null);
					}
				}
			}
			else
			
			{
				ret.Pairs = null;
			}
		}
		return ret;
	}
}
