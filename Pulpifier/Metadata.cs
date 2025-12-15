using System.Text.Json;

namespace Pulp.Pulpifier;

public class Metadata {
	public required string Title { get; init; }
	public required string Author { get; init; }
	public required int Year { get; init; }
	public required string Source { get; init; }
	public required int Words { get; init; }
	public required string Repo { get; init; }
	public required string[] Links { get; init; }
	public required string Blurb { get; init; }

	public static Metadata Parse(string json) {
		Metadata metadata = JsonSerializer.Deserialize<Metadata>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
		if (metadata.Title == "" || metadata.Author == "" || metadata.Year == 0 || metadata.Source == "" || metadata.Words == 0 || metadata.Repo == "" || metadata.Links.Length == 0 || metadata.Blurb == "") throw new Exception("Missing required metadata.");
		return metadata;
	}
}