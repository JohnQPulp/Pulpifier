using System.Text.RegularExpressions;

namespace Pulp.Pulpifier;

public static class Compiler {
	public static bool TryBuildHTML(string rawText, string pulpText, out string html) {
		try {
			html = BuildHTML(rawText, pulpText);
			return true;
		} catch (Exception e) {
			html = "";
			return false;
		}
	}

	public static string BuildHTML(string rawText, string pulpText) {
		rawText = Regex.Replace(rawText, @"[\r\n]+", " ");
		string[] allLines = pulpText.Split(["\r\n", "\n"], StringSplitOptions.None);

		List<string> htmlSnippets = new();

		int rawIndex = 0;
		for (int i = 0; i < allLines.Length; i += 3) {
			string line = allLines[i];
			int endIndex = rawIndex + line.Length;
			if (rawText[rawIndex..endIndex] != line) {
				throw new Exception("Line mismatch at line " + i);
			}

			rawIndex = endIndex + 1;
			htmlSnippets.Add("<p>" + allLines[i] + "</p>");
		}

		return string.Join('\n', htmlSnippets);
	}
}