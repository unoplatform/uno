using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Samples.Samples.Shared.Content.UITests
{
	[Sample("Brushes", Description = "List with ImageBrush background in items")]
	public sealed partial class ImageBrushInList : UserControl
	{
		public ImageBrushInList()
		{
			this.InitializeComponent();
		}

		public ListItem[] SampleItems { get; } = _sampleItems;

		public class ListItem
		{
			public ListItem(string uri)
			{
				Uri = uri;
			}

			public string Uri { get; private set; }

			public static implicit operator ListItem(string uri)
			{
				return new ListItem(uri);
			}
		}

		private static readonly ListItem[] _sampleItems = {
				"http://lh5.ggpht.com/mKuevNSOL2rtQ932ZamBfXPkvBd-YIo6Odv7wXeQt8U-DW4c6zFutDbLMr6tWuaD6kappbIcgnPK-9TNKs9HRPwF5K1-cH20YOqx5WRO",
				"http://lh5.ggpht.com/jDipyIb4UimkT_xR6sDZ63qOsGvxvwGJ2AyxnVq2UiG3Z_RSXUJU9NSkOFnoMLJxT2AcI_h1ePjRbGqFE5c_cnXmqRQRXuq3-vpNCavf",
				"http://lh6.ggpht.com/CiCocGppE7iuYyNCf4b_MFm9LKCs1KMSkapB0A_CFu5tgHCkoQVwYdpzpjdHj6RzR7RAUp2oJaZiBguW-EsrKeYH2a19nxIEaNVxSueL",
				"http://lh3.ggpht.com/o3D4oaj_j0RhaVPaJo1nmLZDW5A6gfsflFJ-buw3qdrrPtsSo2E6dgJ6Yu-cPToGc9KGo6Gj1X0NQ9BV2oCM14-21ObDTYatLYEK6xPXlw",
				"http://lh4.ggpht.com/C7mtHaKoVyLzz6x8P2GCMmGASSiw5NEiKSid0dVEhI6_GcRB80fY5U2rkyNgy64GlSmcQT1SMl48BSoObGhWqU3xehC8I4QIXG-Py04o",
				"http://lh3.ggpht.com/XnVuDMqY67N6Q0nhgU3bXxvVDjuYQyB49NRCCkqnvLc1s-_p0G6LO51ttX_XGeEy5BxBxQsEVIenSS-v1VRANgfurm1EYlRlf2eKD0iK",
				"http://lh4.ggpht.com/zV39Vvj5cVgQQqO1_XnFUmm-eTA_lOVf407a_cGkpDoLoS5F-tWLVKfPFXXKOVzj_bdXSsCisF6rTeObz5TtaySX-bdJGunGKbffzJLm",
				"http://lh3.ggpht.com/IVvJ4Caur-PWr_3YeBwPbwIBXyXutAj4TOSY-f1IjNtj79E5R6QE0rQhCnVbxylCD7pqX87FTF0hfEldwNualvq7Y1LOlVZ56gG9G0Wx",
				"http://lh5.ggpht.com/V2JXjy7suZ56ihIOD2gvspmxVsybv0VkSJqJNgBnQyCXPXW4ZmVJZ8I584w2mpSmyf6FT9Vn7UG7g7BvmpUrDdMBLYb8QuRbTPqDvns",
				"http://lh3.ggpht.com/_tqOkcHTmAXJvV4h_4HirNtZnxH3T5oNlXHlfFPguyrAxR-LDGhQBEZ9tF5dx2pYmKNM9abVXRNsFuku93gJS-RYCwUAiAdhyBy70bxgjQ",
				"http://lh6.ggpht.com/HCpq-Zgku83SFF_XNcMrj9t133JSEjV9jToOwSPiiBpysr-_aXjN3UzI43hulh8SX8zinSD8Uq2Dkw5Ce8-emgOvGHiictPqbuJ7dFYo",
				"http://lh4.ggpht.com/oMUgbu7h4Finun7gBS4e0VjSnNCiEiqAubguAbQx8phtvM67_WiG1X8IKzeGc2_bKNZhcqT7PsTT5okuXcjiBW3WXCAUmaex_n_vEg",
				"http://lh5.ggpht.com/6rrW-nmeUQYThPhB_JE4HnS4aSLkSqU71zlVA7Y9nbAvOjHdww5kCQ_hAsu5OWSJuBed_K3w0mQ3IKwWrH3O9RCB2wPt1Gd9CZWKxg6xTQ",
				"http://lh6.ggpht.com/OJhIm8Yf1PPLLpqXuxmGc1UbCvvDiQCR9zr5QrCR1ycFdGerIg-xiYOUQ4szb00gc0VDvE2JCMMXi7JsvOvs84SLRWk9Hxok75T3CNiY",
				"http://lh4.ggpht.com/qW70zwJ8Qqy7KiLDEIjIYLa_3KgI6IdAtfQ5MxvyhNCgqc8BDWXYNMxAx5ArCSOnsETE6S0WRZD6qlnzpR6--QfCvtKm1LaPQDOAIlpI",
				"http://lh5.ggpht.com/1kuO2kic8EABAMJjMH6St7nv7nF51tGJkvB32nR74Vh_IuHZiTc1Mxfbhu2meoohufyYHEjXCoPpLtO1KTd5HfdUD8ZOUrv9Q743pvqXFA",
				"http://lh3.googleusercontent.com/jv9q0VlXMbAx32oOdZADifEiXE-e5WL8tFC7p6U7mVcnNEH2cWtC3YByT4UIGFPBzBCglDSy3tef2pQputfi6GPVairRuBgxK5S662mZ",
				"http://lh3.googleusercontent.com/llh_cWR4HONtmZhkMcWDXxzfLV_plfNADAZGB6wZAb_u19zlAyU3V30BLnf6V6U4Jwpmh8n-pIk7FXRZlZ9UhkhFqM58W8lK4iZV1yoo",
				"http://lh3.googleusercontent.com/wBgH6H7yeLug_TX2Z6m__sWXAJGRed6USVTYWYmCdBdRitLTGuzlZIxm3QpHjJpMReLeGrSZ5AYJWtTm8JvEG-LsHhmsnjltPGLoyWLHsQ",
				"http://lh3.googleusercontent.com/Mk6g2yyld9V0xHTX0RrD0ka8z-mjIbflU2gOPxvte4XTyfsksT0KN3CTQTI8z49a9Q1LpjO9ZS7cBphMOUXsrmjwgOCMbpHocDrXQwXq",
				"http://lh3.googleusercontent.com/5e0jC91Q2IEZK9IGbcEmAKRJYoiwXXP_jmLZKSGgmk_S1yO6l3jr1_DqO6xk-IeqF8kM8HSCq44CwrQ4vJVMzSX97fR8Mb3UBX9FWm4",
				"http://lh3.googleusercontent.com/fCk3QNFXGpXTQRlUpOy5DTc0j1aVpSbUSp9dyShEY3VjWXXiXTlOC4q6pxijhvxXKnj1Vz9B_TNhjWt7ok4c_0bQTH4n2w1WwwTUCgUi",
				"http://lh3.googleusercontent.com/Sm8Un4ofAZ8heOQT8ANY4E9BeIVzdQ3W7sLpGhuoXYFl_OrYpXwGegcnMQV4iEl8QhbQsygxIPJDQsHlCkVy3RHqmD97drERx5R7kfg",
				"http://lh3.googleusercontent.com/_QYopQ2U6iiWQvYMIKv-R6CR6LKBCBqV2_pdtMcGAPT6Esi5S7YbE_RVQiz7Oe5eWs4CF0Zx6d4VNSfivmvW8Go4o7ApBuBdwvSGyFXPfw",
				"http://lh3.googleusercontent.com/snMMaF_WZVB2mArGxDJAwiRzEUPVD3srUGfAM4REaaL0Ogcy2RS6Wcd6NYElPc9__nqx7tVaa3GtRB5RXebB5PH_XwDL2pBKITa61AwP",
				"http://lh3.googleusercontent.com/Zd4mUQ752AbEAAD_8xHs91-5pazv_4KOqvlsZfmpugm-FPrLhcELb7v6F4N73rTVd4sh3eNSa5YeKCIW_WKv1Q_IObdNEAeOsf_l0Y0z",
				"http://lh3.googleusercontent.com/2L3R5r_5Ct-pM3aZTtlcEKjpp3Pd9x9Vd03ZkHfO-Zc8-xL06Gt3lz47sNJn3X6BTTNj5N-2QiOsNT4kGXsQhVwrFtUR01Le79e8g-e7",
				"http://lh3.googleusercontent.com/Mo1l3sRqsukg-HwbqRHyG3dnbJCQlx8ylAdw8h5rLza-md7hMoj4Bx-O4R5bOLwyZst1p_vuj58mgbmjVKKqJrBbSlp3zBSYjbmaatuf0g",
				"http://lh3.googleusercontent.com/m18YA2j5N5fgK-jhiG3DLMGiuLbiNBCU6E9MSiTLysZqHipd9sUkS69Im1WImI0mNUj8v6FygecPee9VnQ5GIXJX6MOYxeDhf_XDRjc",
				"http://lh3.googleusercontent.com/Co_hPYld1uio9kOu2hV2l_Kv8BRzHa8HCnr9LgMjkm574wMU4UcDneVP0lhbD3B9BmNAu_IT8rSaibXiIaAnxByGZh-pPVpVEGEg1iE",
				"http://lh3.googleusercontent.com/jM6H-MSmpYN1ijmF-M76UczcfJMUAq9p1qW0_4ZjQdvripBUyb2jlTIVzdcJnqseJVbz3vyat5oB2LFBPTzfO8VMit5bnIMaMi0BAt4MPw",
				"http://lh3.googleusercontent.com/OORSz7LTD4to8mQQTdpODirk38B8c97uGbYuaPiEAw1z5b2ADj93OJy6mHyu7PL4aNBuoxARAPAQLk8fvIdF2wXclkWyAp3whqR7n206",
				"http://lh3.googleusercontent.com/ms0RnLiDGaxDgtKkrgURP2w5Df1agsmziLYy1hXonKuddem-F0tuzOghne1PUtTGwDKglzxB_BhpA7Eivuodlw2p-NJUg9ncizvVyT3hKg",
				"http://lh3.googleusercontent.com/U4_2SG7qiwoV2Ul8KNE8y64rWDybch-vbrSt1aBeiApJSigKC0cvCUcBXVr2NCQfAaaBMgOYFUyAIN4wnUPsEt6euf8N0TnRtTzl6Nt1Vg"
			};
	}
}
