using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Linq;

namespace AVDump3Lib.Misc;

public static class ExtensionMethods {
	//public static TResult OnNotNullReturn<TResult, TSource>(this TSource? n, Func<TSource, TResult> transform) where TSource : struct {
	//    return n.HasValue ? transform(n.Value) : default(TResult);
	//}
	public static TResult OnNotNullReturn<TResult, TSource>(this TSource n, Func<TSource, TResult> transform) {
		return n != null ? transform(n) : default;
	}
	public static TResult Transform<TResult, TSource>(this TSource n, Func<TSource, TResult> transform) {
		if(n == null) throw new ArgumentNullException(nameof(n));
		if(transform == null) throw new ArgumentNullException(nameof(transform));
		return transform(n);
	}

	public static void OnNotNull<TSource>(this TSource n, Action<TSource> transform) { if(n != null) transform(n); }


	public static string Truncate(this string value, int maxLength) {
		return (value ?? "").Length <= maxLength ? value : value[..maxLength];
	}

	public static void Sort(this XElement source, Comparison<XElement> comparison) {
		//Make sure there is a valid source
		if(source == null) throw new ArgumentNullException(nameof(source));

		//Sort attributes if needed
		//if(bSortAttributes) {
		//	List<XAttribute> sortedAttributes = source.Attributes().OrderBy(a => a.ToString()).ToList();
		//	sortedAttributes.ForEach(a => a.Remove());
		//	sortedAttributes.ForEach(a => source.Add(a));
		//}

		//Sort the children IF any exist
		var sortedChildren = source.Elements().ToList();
		sortedChildren.Sort(comparison);
		if(source.HasElements) {
			source.RemoveNodes();
			//sortedChildren.ForEach(c => c.Sort());
			sortedChildren.ForEach(c => source.Add(c));
		}
	}
}

public class BitConverterEx {
	public static readonly ImmutableDictionary<string, string> Bases = ImmutableDictionary.CreateRange(
		new KeyValuePair<string, string>[] {
				new KeyValuePair<string, string>("2", "01"),
				new KeyValuePair<string, string>("4", "0123"),
				new KeyValuePair<string, string>("8", "01234567"),
				new KeyValuePair<string, string>("10", "0123456789"),
				new KeyValuePair<string, string>("16", "0123456789ABCDEF"),
				new KeyValuePair<string, string>("32Hex", "0123456789ABCDEFGHIJKLMNOPQRSTUV"),
				new KeyValuePair<string, string>("32Z", "0123456789ABCDEFGHJKMNPQRSTVWXYZ"),
				new KeyValuePair<string, string>("32", "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"),
				new KeyValuePair<string, string>("36", "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"),
				new KeyValuePair<string, string>("62", "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"),
				new KeyValuePair<string, string>("64", "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/")
		}
	);



	public static string ToBase(byte[] valueAsArray, string digits, int pad = -1) {
		if(digits == null) throw new ArgumentNullException(nameof(digits));
		if(digits.Length < 2) throw new ArgumentOutOfRangeException(nameof(digits), "Expected string with at least two digits");

		if(pad == -1) pad = (int)Math.Ceiling(Math.Log(1L << (8 * valueAsArray.Length), digits.Length));

		var value = new BigInteger(valueAsArray, true, true);
		var sb = new StringBuilder(pad);

		do {
			value = BigInteger.DivRem(value, digits.Length, out var rem);
			sb.Append(digits[(int)rem]);
		} while(value > 0);

		if(sb.Length < pad) sb.Append(digits[0], pad - sb.Length);

		// reverse it
		for(int i = 0, j = sb.Length - 1; i < j; i++, j--) {
			(sb[j], sb[i]) = (sb[i], sb[j]);
		}

		return sb.ToString();

	}


}
