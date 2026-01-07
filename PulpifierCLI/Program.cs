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
		filesToPrint.Sort();

		string imageDir = Path.Combine(directory, "images");
		foreach (string file in filesToPrint) {
			bool found = File.Exists(Path.Combine(imageDir, $"{file}.webp"));
			Console.WriteLine($"{imageFiles[file],5} {file}{(found ? "" : " (missing)")}");
		}
	} else {
		throw new Exception($"Invalid arg \"{args[1]}\".");
	}
}