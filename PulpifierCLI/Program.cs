using System.Text.RegularExpressions;
using Pulp.Pulpifier;

string directory = Path.GetFullPath(args[0], Directory.GetCurrentDirectory());
string jsonText = File.ReadAllText(Path.Combine(directory, "metadata.json"));
Metadata.Parse(jsonText);

string rawText = File.ReadAllText(Path.Combine(directory, "book.txt"));
string pulpText = File.ReadAllText(Path.Combine(directory, "pulp.txt"));

string html = Compiler.BuildHtml(rawText, pulpText, out Dictionary<string, int> imageFiles);
File.WriteAllText(Path.Combine(directory, "out.html"), html);

if (args.Length == 1) {
	Console.WriteLine("Written.");
} else if (args.Length == 2) {
	if (args[1] == "-l" || args[1] == "--list-images") {
		List<string> filesToPrint = imageFiles.Keys.ToList();
		Regex r = new Regex(@"^c-([^-]+)(-a[^-]+)?((-x[^-]+)+)?(-e[^-]+)?(-s)?$");
		filesToPrint.Sort((s1, s2) => {
			if (s1.StartsWith("b-") || s1.StartsWith("o-") || s2.StartsWith("b-") || s2.StartsWith("o-")) {
				return string.Compare(s1, s2, StringComparison.Ordinal);
			}

			Match m1  = r.Match(s1);
			Match m2 = r.Match(s2);
			if (!m1.Success || !m2.Success) throw new Exception($"Bad character file name: \"{s1}\", \"{s2}\"");

			for (int i = 1; i <= 6; i++) {
				if (m1.Groups[i].Value != m2.Groups[i].Value) {
					return string.Compare(m1.Groups[i].Value, m2.Groups[i].Value, StringComparison.Ordinal);
				}
			}
			throw new Exception($"Duplicate character entries: \"{s1}\", \"{s2}\"");
		});

		string imageDir = Path.Combine(directory, "images");
		foreach (string file in filesToPrint) {
			bool found = File.Exists(Path.Combine(imageDir, $"{file}.webp"));
			Console.WriteLine($"{imageFiles[file],5} {file}{(found ? "" : " (missing)")}");
		}
	} else {
		throw new Exception($"Invalid arg \"{args[1]}\".");
	}
}