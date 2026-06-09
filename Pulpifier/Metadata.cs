using System.Text.Json;

namespace Pulp.Pulpifier;

public class Metadata {
	public required string Title { get; init; }
	public required string ShortTitle { get; init; }
	public string VNTitle => ShortTitle + ": The Visual Novel";
	public required string Author { get; init; }
	public string? Translator { get; init; }
	public required int Year { get; init; }
	public required string Source { get; init; }
	public required int Words { get; init; }
	public required DateOnly? PulpDate { get; init; }
	public required string Repo { get; init; }
	public required string Wikipedia { get; init; }
	public required string Blurb { get; init; }
	public string? Blurb2 { get; init; }
	public bool? UseAvif { get; init; }
	public string ImageExtension => UseAvif == true ? "avif" : "webp";
	public bool? EditedOkay { get; init; }
	public bool NeedsReediting => !((UseAvif.HasValue && UseAvif.Value) || (EditedOkay.HasValue && EditedOkay.Value));
	public int? AuthorWidth { get; init; }

	public static Metadata Parse(string json) {
		try {
			Metadata metadata = JsonSerializer.Deserialize<Metadata>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
			if (metadata.Title == "" || metadata.ShortTitle == "" || metadata.Author == "" || metadata.Year == 0 || metadata.Words == 0 || metadata.Blurb == "") throw new Exception("Missing required metadata.");
			if (!metadata.Source.StartsWith("https://standardebooks.org/ebooks/") || !metadata.Repo.StartsWith("https://github.com/JohnQPulp/") || !metadata.Wikipedia.StartsWith("https://en.wikipedia.org/wiki/")) throw new Exception("Bad metadata link.");
			if (metadata.PulpDate != null && metadata.PulpDate < new DateOnly(2026, 1, 1)) throw new Exception("Bad date.");
			if (!metadata.Title.Contains(metadata.ShortTitle)) throw new Exception("Bad short title.");
			return metadata;
		} catch {
			Console.WriteLine(json);
			throw;
		}
	}
}