using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AVDump3Lib.Information.MetaInfo.Media {
    public class Chapters : MetaInfoContainer {
		public static readonly MetaInfoItemType IdType = new MetaInfoItemType("Id", null, typeof(int), "");
		public static readonly MetaInfoItemType IsHiddenType = new MetaInfoItemType("IsHidden", null, typeof(bool), "");
		public static readonly MetaInfoItemType IsDefaultType = new MetaInfoItemType("IsDefault", null, typeof(bool), "");
		public static readonly MetaInfoItemType IsOrderedType = new MetaInfoItemType("IsOrdered", null, typeof(bool), "");
		public static readonly MetaInfoItemType ChapterType = new MetaInfoItemType("Chapter", null, typeof(Chapter), "");

        public Chapters() : base(MediaProvider.ChaptersType) {        }
	}

	public class Chapter : MetaInfoContainer {
		public static readonly MetaInfoItemType ChapterType = new MetaInfoItemType("Chapter", null, typeof(Chapter), "");

		public static readonly MetaInfoItemType IdType = new MetaInfoItemType("Id", null, typeof(int), "");
		public static readonly MetaInfoItemType IdStringType = new MetaInfoItemType("IdString", null, typeof(string), "");

		public static readonly MetaInfoItemType TimeStartType = new MetaInfoItemType("TimeStart", "byte", typeof(int), "");
		public static readonly MetaInfoItemType TimeEndType = new MetaInfoItemType("TimeStart", "byte", typeof(int), "");

		public static readonly MetaInfoItemType SegmentIdType = new MetaInfoItemType("SegmentId", null, typeof(byte[]), "");
		public static readonly MetaInfoItemType SegmentChaptersIdType = new MetaInfoItemType("SegmentChaptersId", null, typeof(int), "");

		public static readonly MetaInfoItemType PhysicalEquivalentType = new MetaInfoItemType("PhysicalEquivalent", null, typeof(int), "");

		public static readonly MetaInfoItemType AssociatedTrackType = new MetaInfoItemType("AssociatedTrack", null, typeof(int), "");

		public static readonly MetaInfoItemType IsHiddenType = new MetaInfoItemType("IsHidden", null, typeof(bool), "");
		public static readonly MetaInfoItemType IsEnabledType = new MetaInfoItemType("IsEnabled", null, typeof(bool), "");

		public static readonly MetaInfoItemType TitleType = new MetaInfoItemType("Title", null, typeof(ChapterTitle), "");

		public static readonly MetaInfoItemType HasOperationsType = new MetaInfoItemType("HasOperations", null, typeof(bool), "");

        public Chapter() : base(Chapters.ChapterType) {   }
	}

	public class ChapterTitle {
		public ChapterTitle(string title, IEnumerable<string> languages, IEnumerable<string> countries) {
			Title = title;
			Languages = languages.ToList().AsReadOnly();
			Countries = countries.ToList().AsReadOnly();
		}
		public string Title { get; private set; }
		public ReadOnlyCollection<string> Languages { get; private set; }
		public ReadOnlyCollection<string> Countries { get; private set; }
	}

}
