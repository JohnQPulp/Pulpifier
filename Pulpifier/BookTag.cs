using System.Text.RegularExpressions;

namespace Pulp.Pulpifier;

public static partial class BookTag {
	public static string FormatText(string text) {
		return BookRegex().Replace(text, m => {
			string book = m.Groups[1].Value;
			string text = m.Groups[2].Value.Trim('|');
			string url = "https://www.goodreads.com/search?q=" + Uri.EscapeDataString(book.ToLowerInvariant());
			return text == "" ? $"<i><a href='{url}'>{book}</a></i>" : $"<a href='{url}'>{text}</a>";
		});
	}

    [GeneratedRegex(@"<book>(.*?)(\|.*?)?</book>", RegexOptions.Singleline)]
    private static partial Regex BookRegex();
}