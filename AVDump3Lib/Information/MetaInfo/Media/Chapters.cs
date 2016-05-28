using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AVDump3Lib.Information.MetaInfo.Media {
    public class Chapters : MetaInfoContainer {
        public static readonly MetaInfoItemType<int> IdType = new MetaInfoItemType<int>("Id", null);
        public static readonly MetaInfoItemType<bool> IsHiddenType = new MetaInfoItemType<bool>("IsHidden", null);
        public static readonly MetaInfoItemType<bool> IsDefaultType = new MetaInfoItemType<bool>("IsDefault", null);
        public static readonly MetaInfoItemType<bool> IsOrderedType = new MetaInfoItemType<bool>("IsOrdered", null);
        public static readonly MetaInfoItemType<Chapter> ChapterType = new MetaInfoItemType<Chapter>("Chapter", null);


        public Chapters(int id) : base(id) {
        }
    }

    public class Chapter : MetaInfoContainer {
        public static readonly MetaInfoItemType<Chapter> ChapterType = new MetaInfoItemType<Chapter>("Chapter", null);

        public static readonly MetaInfoItemType<int> IdType = new MetaInfoItemType<int>("Id", null);
        public static readonly MetaInfoItemType<string> IdStringType = new MetaInfoItemType<string>("IdString", null);

        public static readonly MetaInfoItemType<double> TimeStartType = new MetaInfoItemType<double>("TimeStart", "byte");
        public static readonly MetaInfoItemType<double> TimeEndType = new MetaInfoItemType<double>("TimeStart", "byte");

        public static readonly MetaInfoItemType<byte[]> SegmentIdType = new MetaInfoItemType<byte[]>("SegmentId", null);
        public static readonly MetaInfoItemType<int> SegmentChaptersIdType = new MetaInfoItemType<int>("SegmentChaptersId", null);

        public static readonly MetaInfoItemType<int> PhysicalEquivalentType = new MetaInfoItemType<int>("PhysicalEquivalent", null);

        public static readonly MetaInfoItemType<int> AssociatedTrackType = new MetaInfoItemType<int>("AssociatedTrack", null);

        public static readonly MetaInfoItemType<bool> IsHiddenType = new MetaInfoItemType<bool>("IsHidden", null);
        public static readonly MetaInfoItemType<bool> IsEnabledType = new MetaInfoItemType<bool>("IsEnabled", null);

        public static readonly MetaInfoItemType<ChapterTitle> TitleType = new MetaInfoItemType<ChapterTitle>("Title", null);

        public static readonly MetaInfoItemType<bool> HasOperationsType = new MetaInfoItemType<bool>("HasOperations", null);

        public Chapter(int id) : base(id) { }
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
