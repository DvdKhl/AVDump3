using System;
using System.Collections.ObjectModel;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg;

public interface IVorbisComment {
	Comments Comments { get; }
}

internal class VorbisCommentParser {
	public bool FullyRead { get; private set; }
	public bool ContainsComments { get; private set; }

	private byte[] data;
	private int dataLength;

	private readonly byte[] header = new byte[] { 0x03, (byte)'v', (byte)'o', (byte)'r', (byte)'b', (byte)'i', (byte)'s' };


	public void ParsePage(ref OggPage page) {
		if(FullyRead) return;

		if(!ContainsComments && page.Data.IndexOf(header) != -1) ContainsComments = true;
		if(!ContainsComments) return;

		var commentData = page.Data;

		if(data == null) data = new byte[commentData.Length * 5];
		else if(data.Length - dataLength < commentData.Length) Array.Resize(ref data, (data.Length + commentData.Length) * 2);

		commentData.CopyTo(((Span<byte>)data)[dataLength..]);
		dataLength += commentData.Length;


		if((page.Flags & PageFlags.SpanAfter) != 0) {
			FullyRead = true;
		}
	}

	public Comments RetrieveComments() {
		if(ContainsComments) {
			return Comments.Parse(data);
		} else {
			return new Comments();
		}

	}
}


public class Comments {
	public string Vendor { get; private set; }
	public CommentCollection Items { get; set; }

	internal Comments() { Items = new CommentCollection(); }

	public static Comments Parse(byte[] b) {
		var ch = new Comments();
		int offset = 7, length, count;

		length = BitConverter.ToInt32(b, offset); offset += 4;
		ch.Vendor = System.Text.Encoding.UTF8.GetString(b, offset, length); offset += length;
		count = BitConverter.ToInt32(b, offset); offset += 4;

		for(var i = 0; i < count; i++) {
			length = BitConverter.ToInt32(b, offset);
			offset += 4;

			var comment = Comment.Parse(System.Text.Encoding.UTF8.GetString(b, offset, length));
			offset += length;

			if(ch.Items.Contains(comment.Key)) {
				ch.Items[comment.Key].Value.Add(comment.Value[0]);
			} else {
				ch.Items.Add(comment);
			}
		}

		return ch;
	}

	public class CommentCollection : KeyedCollection<string, Comment> { protected override string GetKeyForItem(Comment item) { return item.Key; } }
}
public class Comment {
	public string Key { get; private set; }
	public Collection<string> Value { get; private set; }

	private Comment() { Value = new Collection<string>(); }

	public static Comment Parse(string commentStr) {
		var pos = commentStr.IndexOf('=');
		Comment comment;
		if(pos < 0) {
			comment = new Comment { Key = "undefined" };
			comment.Value.Add(commentStr);
		} else {
			comment = new Comment { Key = commentStr[..pos].ToLower() };
			comment.Value.Add(commentStr[(pos + 1)..]);
		}
		return comment;
	}
}
