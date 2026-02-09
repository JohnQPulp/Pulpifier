using System.Text.Json;

namespace Pulp.Pulpifier;

public class Metadata {
	public required string Title { get; init; }
	public required string ShortTitle { get; init; }
	public string VNTitle => ShortTitle + ": The Visual Novel";
	public required string Author { get; init; }
	public required int Year { get; init; }
	public required string Source { get; init; }
	public required int Words { get; init; }
	public required DateOnly? PulpDate { get; init; }
	public required string Repo { get; init; }
	public required Dictionary<string, string> Links { get; init; }
	public required string Blurb { get; init; }
	public string? Blurb2 { get; init; }

	public static Metadata Parse(string json) {
		Metadata metadata = JsonSerializer.Deserialize<Metadata>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		if (metadata.Title == "" || metadata.ShortTitle == "" || metadata.Author == "" || metadata.Year == 0 || metadata.Source == "" || metadata.Words == 0 || metadata.Repo == "" || metadata.Links.Count == 0 || metadata.Links.Any(kvp => kvp.Key == "" || kvp.Value == "") || metadata.Blurb == "") throw new Exception("Missing required metadata.");
		if (metadata.PulpDate != null && metadata.PulpDate < new DateOnly(2026, 1, 1)) throw new Exception("Bad date.");
		return metadata;
	}
}