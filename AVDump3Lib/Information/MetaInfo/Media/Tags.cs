using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AVDump3Lib.Information.MetaInfo.Media {
    public class Tags : MetaInfoContainer {
        public static readonly MetaInfoItemType<TargetedTag> TargetedTagType = new MetaInfoItemType<TargetedTag>("TargetedTag", null);
        //public static readonly MetaInfoItemType SimpleTagType = new MetaInfoItemType("SimpleTag", null, typeof(Tag), "");
        //public static readonly MetaInfoItemType TargetedTagsType = new MetaInfoItemType("TargetedTags", null, typeof(TargetedTags), "");

        public Tags() { }
    }



    public class TargetedTag {
        public string TargetTitle { get; private set; }
        public TargetedTagType TargetType { get; private set; }

        public ReadOnlyCollection<Tag> Tags { get; private set; }
        public ReadOnlyCollection<Target> Targets { get; private set; }

        public TargetedTag(IEnumerable<Target> targets, IEnumerable<Tag> tags) {
            Targets = targets.ToList().AsReadOnly();
            Tags = tags.ToList().AsReadOnly();
        }
    }

    public class Tag {
        public ReadOnlyCollection<Tag> Children { get; private set; }

        public string Name { get; private set; }
        public object Value { get; private set; }
        public string Language { get; private set; }
        public bool IsDefault { get; private set; }

        public Tag(string name, object value, string language, bool isDefault, IEnumerable<Tag> children) {
            Name = name;
            Value = value;
            Language = language;
            IsDefault = isDefault;
            Children = children.ToList().AsReadOnly();
        }

    }

    public class Target {
        public TagTarget Type { get; private set; }
        public long Id { get; private set; }

        public Target(TagTarget type, long id) {
            Type = type;
            Id = id;
        }
    }

    public enum TagTarget { Track, Chapters, Chapter, Attachment }

    //public class TargetedTags : MetaInfoContainer {
    //	public static readonly MetaInfoItemType TypeType = new MetaInfoItemType("Type", null, typeof(TargetedTagType), "");
    //	public static readonly MetaInfoItemType TitleType = new MetaInfoItemType("Type", null, typeof(string), "");
    //	public static readonly MetaInfoItemType TrackIdType = new MetaInfoItemType("TrackId", null, typeof(int), "");
    //	public static readonly MetaInfoItemType ChaptersIdType = new MetaInfoItemType("TrackId", null, typeof(int), "");
    //	public static readonly MetaInfoItemType ChapterIdType = new MetaInfoItemType("TrackId", null, typeof(int), "");
    //	public static readonly MetaInfoItemType AttachmentIdType = new MetaInfoItemType("TrackId", null, typeof(int), "");
    //
    //	public static readonly MetaInfoItemType TagType = new MetaInfoItemType("Tag", null, typeof(Tag), "");
    //}
    public enum TargetedTagType { Instant = 10, Scene = 20, ChapterOrTrack = 30, Session = 40, EpisodeOrAlbum = 50, SeasonOrVolume = 60, Collection = 70 }

}
