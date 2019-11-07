/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetStylesParams
{
	/* Pack=4 */
	HtmlId : number;
	SetAsArranged : boolean;
	Pairs_Length : number;
	Pairs : Array<string>;
	ClipToBounds : boolean;
	public static unmarshal(pData:number) : WindowManagerSetStylesParams
	{
		let ret = new WindowManagerSetStylesParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.SetAsArranged = Boolean(Module.getValue(pData + 4, "i32"));
		}
		
		{
			ret.Pairs_Length = Number(Module.getValue(pData + 8, "i32"));
		}
		
		{
			var pArray = Module.getValue(pData + 12, "*");
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
		
		{
			ret.ClipToBounds = Boolean(Module.getValue(pData + 16, "i32"));
		}
		return ret;
	}
}
