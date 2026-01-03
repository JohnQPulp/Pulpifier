using System.Text.RegularExpressions;

namespace Pulp.Pulpifier;

public static partial class BookTag {
	public static string FormatText(string text) {
		return BookRegex().Replace(text, m => {
			string book = m.Groups[1].Value;
			string text = m.Groups[2].Value.Trim('|');
			string url = "https://www.goodreads.com/search?q=" + Uri.EscapeDataString(book.Trim('"').ToLowerInvariant());
			if (text != "") {
				return $"<a href='{url}'>{text}</a>";
			}
			if (book.StartsWith('"') && book.EndsWith('"')) {
				return $"<a href='{url}'>{book}</a>";
			}
			return $"<i><a href='{url}'>{book}</a></i>";
		});
	}

	[GeneratedRegex(@"<book>(.*?)(\|.*?)?</book>", RegexOptions.Singleline)]
	private static partial Regex BookRegex();
}