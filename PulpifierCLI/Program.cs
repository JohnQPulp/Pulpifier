using Pulp.Pulpifier;

Console.WriteLine("Pulpifier");

string directory = Path.GetFullPath(args[0], Directory.GetCurrentDirectory());
string rawText = File.ReadAllText(Path.Combine(directory, "book.txt"));
string pulpText = File.ReadAllText(Path.Combine(directory, "pulp.txt"));

string html = Compiler.BuildHtml(rawText, pulpText);
File.WriteAllText(Path.Combine(directory, "out.html"), html);

Console.WriteLine("Written.");