using System.Text.Json;
using System.Text.RegularExpressions;

namespace Pulp.Pulpifier;

public static partial class BookTag {
	private static readonly Dictionary<Regex, string> BookUrls;
	static BookTag() {
		Dictionary<string, string> urls = JsonSerializer.Deserialize<Dictionary<string, string>>(Compiler.ReadResource("books.json"))!;
		BookUrls = new Dictionary<Regex, string>();
		foreach (KeyValuePair<string, string> kvp in urls) {
			BookUrls[new Regex($"^\"?{kvp.Key}\"?$")] = kvp.Value;
		}
	}

	public static string FormatText(string text) {
		return BookRegex().Replace(text, m => {
			string book = m.Groups[1].Value;
			string label = m.Groups[2].Value.Trim('|');
			string url = BookUrls.Single(kvp => kvp.Key.IsMatch(book)).Value;
			if (label != "") {
				return $"<a href='{url}'>{label}</a>";
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