using Pulp.Pulpifier;

Console.WriteLine("Pulpifier");

string directory = Path.GetFullPath(args[0], Directory.GetCurrentDirectory());
string rawText = File.ReadAllText(Path.Combine(directory, "book.txt"));
string pulpText = File.ReadAllText(Path.Combine(directory, "pulp.txt"));

string html = Compiler.BuildHtml(rawText, pulpText, out Dictionary<string, int> imageFiles);
File.WriteAllText(Path.Combine(directory, "out.html"), html);

Console.WriteLine("Written.");

if (args.Length == 2) {
	int imagesTo = int.Parse(args[1]);
	List<string> filesToPrint = imageFiles.Where(kvp => kvp.Value < imagesTo).Select(kvp => kvp.Key).ToList();
	filesToPrint.Sort();
	foreach (string file in filesToPrint) {
		Console.WriteLine(file);
	}
}