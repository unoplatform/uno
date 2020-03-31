/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class When_ArrayOfIntParams
{
	/* Pack=1 */
	public MyArray_Length : number;
	public MyArray : Array<number>;
	public static unmarshal(pData:number) : When_ArrayOfIntParams
	{
		const ret = new When_ArrayOfIntParams();
		
		{
			ret.MyArray_Length = Number(Module.getValue(pData + 0, "i32"));
		}
		
		{
			const pArray = Module.getValue(pData + 4, "*");
			if(pArray !== 0)
			{
				ret.MyArray = new Array<number>();
				for(var i=0; i<ret.MyArray_Length; i++)
				{
					const value = Module.getValue(pArray + i * 4, "*");
					ret.MyArray.push(Number(value));
				}
			}
			else
			
			{
				ret.MyArray = null;
			}
		}
		return ret;
	}
}
