using System.Diagnostics;
using System.Text.RegularExpressions;
using Pulp.Pulpifier;

string directory = Path.GetFullPath(args[0], Directory.GetCurrentDirectory());
string jsonText = File.ReadAllText(Path.Combine(directory, "metadata.json"));
Metadata.Parse(jsonText);

string rawText = File.ReadAllText(Path.Combine(directory, "book.txt"));
string pulpText = File.ReadAllText(Path.Combine(directory, "pulp.txt"));

string html = Compiler.BuildHtml(rawText, pulpText, out Dictionary<string, ImageMetadata> imageFiles);
File.WriteAllText(Path.Combine(directory, "out.html"), html);

if (args.Length == 1) {
	Console.WriteLine("Written.");
} else {
	Regex r = new Regex(@"^c-([^-]+)(-a[^-]+)?((-x[^-]+)+)?(-e[^-]+)?(-s[23]?)?$");
	if (args.Length == 2 && (args[1] == "-l" || args[1] == "--list-images")) {
		List<string> filesToPrint = imageFiles.Keys.ToList();
		filesToPrint.Sort((s1, s2) => {
			if (s1.StartsWith("b-") && s2.StartsWith("b-")) {
				return imageFiles[s1].PulpLine - imageFiles[s2].PulpLine;
			}

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

			if (s1 == s2) return 0;

			throw new Exception($"Unexpected comparison failure: \"{s1}\", \"{s2}\"");
		});

		string imageDir = Path.Combine(directory, "images");
		foreach (string file in filesToPrint) {
			bool found = File.Exists(Path.Combine(imageDir, $"{file}.webp"));
			int? fore = imageFiles[file].ForegroundPulpLine;
			Debug.Assert(fore == null || file.StartsWith("b-"));
			Console.WriteLine($"{fore,5} {imageFiles[file].PulpLine,5} {file}{(found ? "" : " (missing)")}");
		}
	} else if (args.Length == 3 && args[1] == "-c") {
		string charBase = "c-" + args[2];
		Match m1 = r.Match(charBase);
		if (!m1.Success) throw new Exception($"Bad character file name: \"{charBase}\"");
		foreach (string file in imageFiles.Keys) {
			if (file.StartsWith("c-")) {
				Match m2 = r.Match(file);
				if (!m2.Success) throw new Exception($"Bad character file name: \"{file}\"");
				if (m1.Groups[1].Value == m2.Groups[1].Value && m1.Groups[2].Value == m2.Groups[2].Value && m1.Groups[3].Value == m2.Groups[3].Value && m1.Groups[4].Value == m2.Groups[4].Value) {
					if (m2.Groups[5].Value != "" || m2.Groups[6].Value != "") {
						Console.WriteLine($"{m2.Groups[5].Value}{m2.Groups[6].Value}");
					}
				}
			}
		}
	} else {
		throw new Exception($"Invalid arg \"{args[1]}\".");
	}
}