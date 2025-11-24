using System.Text;
using System.Text.RegularExpressions;

namespace Pulp.Pulpifier;

public static class Compiler {
	public static bool TryBuildHtml(string rawText, string pulpText, out string html) {
		try {
			html = BuildHtml(rawText, pulpText);
			return true;
		} catch (Exception e) {
			html = "";
			return false;
		}
	}

	public static string BuildHtml(string rawText, string pulpText) {
		StringBuilder sb = new StringBuilder();
		sb.Append("<div><div id='text'></div><button onclick=\"document.getElementById('text').innerHTML = htmlArr[i++];\">Next</button><script>let i = 0; const htmlArr = [");

		string[] rawLines = rawText.Split('\n');
		string[] pulpLines = pulpText.Split('\n');

		Dictionary<string, string> characterNames = new();
		Dictionary<string, string> characterExpressions = new();
		string[] activeCharacters = [];
		string activeSpeaker = "";
		string activeBackground = "";

		int r = 0, p = 0;
		try {
			while (r < rawLines.Length) {
				ThrowIfContainsInvalidChars(rawLines[r]);
				string rawLine = rawLines[r] + ' ';
				if (rawLines[r + 1] != string.Empty) throw new Exception("Missing raw line break.");

				string constructedLine = string.Empty;
				while (rawLine != constructedLine) {
					if (!rawLine.StartsWith(constructedLine)) throw new Exception("Mismatched pulp line.");

					string pulpLine = pulpLines[p];

					sb.Append('`');
					sb.Append(pulpLine);
					sb.Append("`, ");

					constructedLine += Regex.Replace(pulpLine, "<e>.*?</e>", "") + ' ';
					if (pulpLines[p + 2] != string.Empty) throw new Exception("Missing pulp line break.");

					string[] metadata = pulpLines[p + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					foreach (string data in metadata) {
						string[] kvp = data.Split('=');
						string key = kvp[0];
						string value = kvp[1];

						switch (key[0]) {
							case 'n':
								string name = key.Split(':')[1];
								characterNames[name] = value;
								break;
							case 'c':
								if (value == "") {
									activeCharacters = [];
								} else {
									string[] characters = value.Split(',');
									if (characters.Any(c => !characterNames.ContainsKey(c))) throw new Exception("Missing character name.");
									activeCharacters = characters;
								}
								break;
							case 'b':
								activeBackground = value;
								break;
							case 'e':
								string ename = key.Split(':')[1];
								if (!characterNames.ContainsKey(ename)) throw new Exception("Missing character name for expression.");
								characterExpressions[ename] = value;
								break;
							case 's':
								if (value != "" && !characterNames.ContainsKey(value)) throw new Exception("Missing character name for speaker.");
								activeSpeaker = value;
								break;
							default: throw new Exception($"Unrecognized key: '{key}'.");
						}
					}

					p += 3;
				}

				r += 2;
			}
		} catch (Exception e) {
			string error = $"Parsing error at raw line {r} and pulp line {p}.";
			if (rawLines.Length > r) error += '\n' + rawLines[r];
			if (pulpLines.Length > p) error += '\n' + pulpLines[p];
			if (pulpLines.Length > p + 1) error += '\n' + pulpLines[p + 1];
			throw new Exception(error, e);
		}

		sb.Append("];</script></div>");
		return sb.ToString();
	}

	private static readonly char[] InvalidChars = ['`', '“', '”', '‘', '’'];
	private static void ThrowIfContainsInvalidChars(string line) {
		if (line.Any(char.IsControl)) throw new Exception("Contains control char.");
		if (line.ContainsAny(InvalidChars)) throw new Exception("Contains invalid char.");
		if (line.Contains("...") || line.Contains("--")) throw new Exception("Contains invalid sequence.");
		if (line != line.Trim()) throw new Exception("Contains leading/trailing whitespace.");
	}
}