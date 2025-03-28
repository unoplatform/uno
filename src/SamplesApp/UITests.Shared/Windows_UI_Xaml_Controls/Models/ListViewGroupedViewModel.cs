using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
using Uno.Extensions;
using Uno.UI.Samples.UITests.Helpers;

namespace SamplesApp.Windows_UI_Xaml_Controls.Models
{
	internal class ListViewGroupedViewModel : ViewModelBase
	{
		Random _rnd = null;
		private bool _areStickyHeadersEnabled;
		private double _variableWidth;

		public IEnumerable<IGrouping<string, string>> GroupedSampleItems { get; }
		public IEnumerable<object> GroupedSampleItemsAsSource { get; }
		public IEnumerable<IGrouping<int, int>> GroupedNumericItems { get; }
		public IEnumerable<object> GroupedNumericItemsAsSource { get; }
		public string[] UngroupedSampleItems { get; }
		public bool AreStickyHeadersEnabled
		{
			get => _areStickyHeadersEnabled;
			set
			{
				_areStickyHeadersEnabled = value;
				RaisePropertyChanged();
			}
		}

		public IEnumerable<object> GroupedTownsAsSource { get; }
		public IEnumerable<object> EmptyGroupsAsSource { get; }
		public IEnumerable<object> EmptyAsSource { get; }

		public double VariableWidth
		{
			get => _variableWidth;
			set
			{
				_variableWidth = value;
				RaisePropertyChanged();
			}
		}

		public ListViewGroupedViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
			_rnd = new Random();

			GroupedSampleItems = GetGroupedSampleItems();
			GroupedSampleItemsAsSource = ListViewViewModel.GetAsCollectionViewSource(GetGroupedSampleItems());
			GroupedNumericItems = GetGroupedNumericItems();
			GroupedNumericItemsAsSource = ListViewViewModel.GetAsCollectionViewSource(GetGroupedNumericItems());
			//TODO: Restore dynamic samples based on IObservable
			//.Attach("ChangingGroupedSampleItems", () => GetChangingGroupedSampleItems())
			//.Attach("ChangingGroupedSampleItemsAsSource",
			//	() => GetChangingGroupedSampleItems()
			//			.SelectMany(s =>
			//				GetAsCollectionViewSource(CancellationToken.None, s).ToObservable()
			//			)
			//)
			UngroupedSampleItems = _sampleItems;
			//.Attach("ChangingIntArray", () => GetChangingIntArray())
			AreStickyHeadersEnabled = true;
			GroupedTownsAsSource = ListViewViewModel.GetAsCollectionViewSource(GetTownsGroupedAlphabetically());
			EmptyGroupsAsSource = ListViewViewModel.GetAsCollectionViewSource(GetAllEmptyGroups());
			EmptyAsSource = ListViewViewModel.GetAsCollectionViewSource(Enumerable.Empty<IGrouping<string, string>>());
			VariableWidth = 500d;
			//	)
			//);
		}

		/// <summary>
		/// Group by first character
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IGrouping<string, string>> GetGroupedSampleItems()
		{
			return _sampleItems
				.GroupBy(s => s[0].ToString().ToUpperInvariant())
				.Concat(new EmptyGroup<string, string>("This header should not appear if GroupStyle.HidesEmptyGroups is true)"));
		}

		private IEnumerable<IGrouping<int, int>> GetGroupedNumericItems()
		{
			return _sampleNumbers
				.GroupBy(i => GetFirstDigit(i))
				.OrderBy(g => g.Key)
				.Concat(new EmptyGroup<int, int>(1111111111)); //Should not appear if GroupStyle.HidesEmptyGroups is true
		}

		private int GetFirstDigit(int number)
		{
			number = Math.Abs(number);
			while (number >= 10)
			{
				number /= 10;
			}
			return number;
		}

		/// <summary>
		/// Group by Random character at interval
		/// </summary>
		/// <returns></returns>
		private IObservable<IEnumerable<IGrouping<string, string>>> GetChangingGroupedSampleItems()
		{
			throw new NotImplementedException();

			//return Observable.Interval(TimeSpan.FromSeconds(3), Schedulers.Default)
			//	.Select((i) =>
			//	{
			//		var stringIndex = _rnd.Next(0, 9);
			//		var itemsToGroup = _rnd.Next(_sampleItems.Length / 2, _sampleItems.Length);
			//		return _sampleItems
			//			.Take(itemsToGroup)
			//			.Select(s => s.Substring(startIndex: stringIndex) + s.Substring(startIndex: 0, length: stringIndex))
			//			.GroupBy(s => s[0].ToString())
			//			.Concat(new EmptyGroup<string, string>("This header should not appear if GroupStyle.HidesEmptyGroups is true)"))
			//			.ToArray();
			//	}
			//	);
		}

		private IObservable<long[]> GetChangingIntArray()
		{
			throw new NotImplementedException();

			//var baseArray = new[] { 1, 2, 3, 4 };
			//return Observable.Interval(TimeSpan.FromSeconds(1), Schedulers.Default)
			//	.Select(i => baseArray.Select(n => (i + 1) * n).ToArray());
		}

		private static IEnumerable<IGrouping<string, string>> GetTownsGroupedAlphabetically()
		{
			return Enumerable.Range(1, 2)
				.Select(x => new EmptyGroup<string, string>(x.ToString()))
				.Concat(
					// left join
					from letter in _alphabet
					join g in SampleTowns.GroupBy(x => x.Substring(0, 1))
						on letter equals g.Key into groupedTowns
					select groupedTowns.FirstOrDefault() ?? new EmptyGroup<string, string>(letter)
				)
				.ToList();
		}

		private static IEnumerable<IGrouping<string, string>> GetAllEmptyGroups()
		{
			return "Blarg".Select(c => new EmptyGroup<string, string>(c.ToString())).ToArray();
		}

		public double[] WidthChoices { get; } = Enumerable.Range(1, 5).Select(i => i * 100d).ToArray();

		private class EmptyGroup<TKey, TValue> : IGrouping<TKey, TValue>
		{
			private TKey _key;

			public EmptyGroup(TKey key)
			{
				_key = key;
			}

			public TKey Key => _key;

			IEnumerator IEnumerable.GetEnumerator() => Enumerable.Empty<object>().GetEnumerator();

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => Enumerable.Empty<TValue>().GetEnumerator();
		}

		#region Data
		//http://www.genomatix.de/online_help/help/sequence_formats.html
		private static readonly string[] _sampleItems =
		{
			"acaagatgcc", "attgtccccc", "ggcctcctgc", "tgctgctgct", "ctccggggcc", "acggccaccg",
			"ctgccctgcc", "cctggagggt", "ggccccaccg", "gccgagacag", "cgagcatatg", "caggaagcgg",
			"caggaataag", "gaaaagcagc", "ctcctgactt", "tcctcgcttg", "gtggtttgag", "tggacctccc",
			"aggccagtgc", "cgggcccctc", "ataggagagg", "aagctcggga", "ggtggccagg", "cggcaggaag",
			"gcgcaccccc", "ccagcaatcc", "gcgcgccggg", "acagaatgcc", "ctgcaggaac", "ttcttctgga",
			"agaccttctc", "ctcctgcaaa", "taaaacctca", "cccatgaatg", "ctcacgcaag", "tttaattaca",
			"gacctgaatc",
		};

		//Randomly generated numbers in ascending order
		private static readonly int[] _sampleNumbers = {
			7   ,
			22  ,
			26  ,
			28  ,
			37  ,
			40  ,
			44  ,
			44  ,
			52  ,
			60  ,
			63  ,
			64  ,
			67  ,
			80  ,
			80  ,
			83  ,
			88  ,
			93  ,
			96  ,
			97  ,
			100 ,
			122 ,
			149 ,
			279 ,
			345 ,
			402 ,
			412 ,
			423 ,
			468 ,
			531 ,
			541 ,
			562 ,
			562 ,
			570 ,
			602 ,
			647 ,
			691 ,
			706 ,
			768 ,
			786 ,
			790 ,
			948 ,
			3045    ,
			4375    ,
			4416    ,
			4433    ,
			4480    ,
			4556    ,
			5303    ,
			6803    ,
			7306    ,
			7872    ,
			8147    ,
			8608    ,
			8608    ,
			8773    ,
			8861    ,
			9137    ,
			9496    ,
			9527    ,
			9588    ,
			10634   ,
			19158   ,
			21625   ,
			23514   ,
			28058   ,
			28973   ,
			44267   ,
			53102   ,
			54042   ,
			56459   ,
			61886   ,
			62438   ,
			65358   ,
			70855   ,
			81404   ,
			84329   ,
			84969   ,
			85605   ,
			95599   ,
			97088   ,
			99806   ,
			160722  ,
			166771  ,
			168447  ,
			190387  ,
			242757  ,
			363452  ,
			368644  ,
			412261  ,
			530593  ,
			549924  ,
			559429  ,
			623817  ,
			624617  ,
			632277  ,
			667537  ,
			679119  ,
			917773  ,
			925980  ,
			933748  ,
			963684  ,
			1385048 ,
			2058718 ,
			2497614 ,
			2603246 ,
			3329059 ,
			3625791 ,
			3696121 ,
			3845281 ,
			4436034 ,
			4846373 ,
			5331608 ,
			5970062 ,
			6213415 ,
			6258987 ,
			6322301 ,
			6455053 ,
			8885995 ,
			9220539 ,
			9478122 ,
			9538614 ,
			12162147    ,
			17151802    ,
			18439055    ,
			21185339    ,
			22803309    ,
			25422435    ,
			28679457    ,
			31789796    ,
			43285868    ,
			45841326    ,
			50402757    ,
			52234354    ,
			54211661    ,
			62172082    ,
			68194080    ,
			84919079    ,
			93235662    ,
			99388950    ,
			116442531   ,
			131554179   ,
			157544922   ,
			197607390   ,
			225394599   ,
			274928479   ,
			289761960   ,
			314791008   ,
			317542181   ,
			362709528   ,
			384344230   ,
			407588906   ,
			535356841   ,
			566826387   ,
			594197015   ,
			767599837   ,
			861758215   ,
			887213405   ,
			962118341   ,
			982398840   ,
		};

		private static readonly string[] _alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".Select(c => c.ToString()).ToArray();

		internal static readonly string[] SampleTowns =
		{
			"L'Île-Dorval",
			"Barkmere",
			"L'Île-Cadieux",
			//"Estérel",
			"Schefferville",
			"Lac-Saint-Joseph",
			"Belleterre",
			"Lac-Sergent",
			"Scotstown",
			"Lac-Delage",
			"Métis-sur-Mer",
			"Duparquet",
			"Murdochville",
			"Daveluyville",
			"Desbiens",
			"Matagami",
			"Chapais",
			"Fossambault-sur-le-Lac",
			"Saint-Joseph-de-Sorel",
			"Saint-Ours",
			"Kingsey Falls",
			"Waterville",
			"Lebel-sur-Quévillon",
			"Léry",
			"Valcourt",
			"Gracefield",
			"Témiscaming",
			"Thurso",
			"Huntingdon",
			"Causapscal",
			"Saint-Basile",
			"Disraeli",
			"Ville-Marie",
			"Cap-Chat",
			"Bedford",
			"Saint-Pamphile",
			"Macamic",
			"Sainte-Marguerite-du-Lac-Masson",
			"Pohénégamook",
			"Bonaventure",
			"Saint-Gabriel",
			"Sainte-Anne-de-Beaupré",
			"Stanstead",
			"Saint-Marc-des-Carrières",
			"Fermont",
			"Senneterre",
			"Cap-Santé",
			"Dégelis",
			"Portneuf",
			"Clermont",
			"Normandin",
			"Paspébiac",
			"Forestville",
			"Richmond",
			"Percé",
			"Beaupré",
			"Malartic",
			"Grande-Rivière",
			"Trois-Pistoles",
			"Dunham",
			"Saint-Pascal",
			"Montréal-Est",
			//"East Angus",
			"New Richmond",
			"Château-Richer",
			"Baie-D'Urfé",
			"Saint-Tite",
			"Neuville",
			"Sutton",
			"Maniwaki",
			"Carleton-sur-Mer",
			"Danville",
			"Berthierville",
			"Métabetchouan–Lac-à-la-Croix",
			"La Pocatière",
			"Waterloo",
			"Rivière-Rouge",
			"Saint-Joseph-de-Beauce",
			"Warwick",
			"Sainte-Anne-de-Bellevue",
			"Montreal West",
			"Témiscouata-sur-le-Lac",
			"Hudson",
			"Cookshire-Eaton",
			"L'Épiphanie",
			"Windsor",
			"Saint-Pie",
			"Richelieu",
			"Brome Lake",
			"Saint-Césaire",
			"Princeville",
			"Charlemagne",
			"Lac-Mégantic",
			"Contrecoeur",
			"Donnacona",
			"Sainte-Catherine-de-la-Jacques-Cartier",
			"Amqui",
			"Beauceville",
			"Port-Cartier",
			"Mont-Joli",
			"Plessisville",
			"Coteau-du-Lac",
			"Sainte-Anne-des-Monts",
			"Asbestos",
			"Hampstead",
			"Brownsburg-Chatham",
			"Saint-Rémi",
			"Baie-Saint-Paul",
			"Delson",
			"Louiseville",
			"Chibougamau",
			"Bromont",
			"Acton Vale",
			"Chandler",
			"La Sarre",
			"Nicolet",
			"Carignan",
			"Farnham",
			//"Otterburn Park",
			"Pont-Rouge",
			"La Malbaie",
			"Notre-Dame-des-Prairies",
			"Coaticook",
			"Lorraine",
			"Bois-des-Filion",
			"Mont-Tremblant",
			"Saint-Raymond",
			"Saint-Sauveur",
			"Marieville",
			"Sainte-Agathe-des-Monts",
			"Roberval",
			"Saint-Félicien",
			"L'Île-Perrot",
			"Notre-Dame-de-l'Île-Perrot",
			"La Tuque",
			"Montmagny",
			"Mercier",
			"Beauharnois",
			"Sainte-Adèle",
			"Prévost",
			"Bécancour",
			"Cowansville",
			"Lachute",
			"Amos",
			"Sainte-Marie",
			"Saint-Colomban",
			"Lavaltrie",
			"Mont-Laurier",
			"Rosemère",
			"Pincourt",
			"Dolbeau-Mistassini",
			"Matane",
			"Sainte-Anne-des-Plaines",
			"Gaspé",
			"Sainte-Marthe-sur-le-Lac",
			"Saint-Basile-le-Grand",
			"L'Ancienne-Lorette",
			"Sainte-Catherine",
			"Saint-Lin-Laurentides",
			"Deux-Montagnes",
			"Saint-Augustin-de-Desmaures",
			"Mont-Saint-Hilaire",
			"Dorval",
			"Saint-Lazare",
			"Rivière-du-Loup",
			"Mount Royal",
			"Beaconsfield",
			"Joliette",
			"Candiac",
			"Westmount",
			"L'Assomption",
			"Beloeil",
			"Varennes",
			"Kirkland",
			"Saint-Lambert",
			"Baie-Comeau",
			"La Prairie",
			"Saint-Constant",
			"Magog",
			"Chambly",
			"Sept-Îles",
			"Thetford Mines",
			"Sainte-Thérèse",
			"Saint-Bruno-de-Montarville",
			"Boisbriand",
			"Sainte-Julie",
			"Pointe-Claire",
			"Alma",
			"Saint-Georges",
			"Val-d'Or",
			"Côte Saint-Luc",
			"Vaudreuil-Dorion",
			"Sorel-Tracy",
			"Salaberry-de-Valleyfield",
			"Boucherville",
			"Rouyn-Noranda",
			"Mirabel",
			"Mascouche",
			"Victoriaville",
			"Saint-Eustache",
			"Châteauguay",
			"Rimouski",
			"Dollard-des-Ormeaux",
			"Shawinigan",
			"Saint-Hyacinthe",
			"Blainville",
			"Granby",
			"Saint-Jérôme",
			"Drummondville",
			"Brossard",
			"Repentigny",
			"Saint-Jean-sur-Richelieu",
			"Terrebonne",
			"Trois-Rivières",
			"Lévis",
			"Saguenay",
			"Sherbrooke",
			"Longueuil",
			"Gatineau",
			"Laval",
			"Québec",
			"Montreal",
		};
		#endregion
	}
}
