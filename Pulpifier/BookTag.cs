using System.Text.Json;
using System.Text.RegularExpressions;

namespace Pulp.Pulpifier;

public static partial class BookTag {
	private static readonly Dictionary<Regex, string> BookUrls;
	private static readonly Dictionary<string, string[]> AuthorWorks;
	static BookTag() {
		Dictionary<string, string> urls = JsonSerializer.Deserialize<Dictionary<string, string>>(Compiler.ReadResource("books.json"))!;
		BookUrls = new Dictionary<Regex, string>();
		foreach (KeyValuePair<string, string> kvp in urls) {
			if (!UrlRegex().IsMatch(kvp.Value)) throw new Exception($"Bad book link for '{kvp.Key}': {kvp.Value}");
			BookUrls[new Regex($"^\"?{kvp.Key}\"?$")] = kvp.Value;
		}

		Dictionary<string, string[]> works = JsonSerializer.Deserialize<Dictionary<string, string[]>>(Compiler.ReadResource("authors.json"))!;
		AuthorWorks = new Dictionary<string, string[]>();
		foreach (KeyValuePair<string, string[]> kvp in works) {
			AuthorWorks[kvp.Key] = kvp.Value.Select(book => CreateBookLink(book)).ToArray();
		}
	}

	public static string FormatText(string text) {
		return BookRegex().Replace(text, m => {
			string book = m.Groups[1].Value;
			string label = m.Groups[2].Value.Trim('|');
			return CreateBookLink(book, label);
		});
	}

	public static string[] GetAuthorLinks(string author) {
		return AuthorWorks[author];
	}

	public static string CreateBookLink(string book, string? label = null) {
		try {
			string url = BookUrls.Single(kvp => kvp.Key.IsMatch(book)).Value;
			if (!string.IsNullOrEmpty(label)) {
				return $"<a href='{url}'>{label}</a>";
			}

			if (book.StartsWith('"') && book.EndsWith('"')) {
				return $"<a href='{url}'>{book}</a>";
			}

			return $"<i><a href='{url}'>{book}</a></i>";
		} catch (Exception ex) {
			throw new Exception($"'{book}' url retrieval error.", ex);
		}
	}

	[GeneratedRegex(@"<book>(.*?)(\|.*?)?</book>", RegexOptions.Singleline)]
	private static partial Regex BookRegex();
	[GeneratedRegex(@"^https://www\.goodreads\.com/book/show/[^/\?]+$")]
	private static partial Regex UrlRegex();
}