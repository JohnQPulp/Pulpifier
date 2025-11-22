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

		int r = 0, p = 0;
		string rawLine = "", pulpLine = "";
		try {
			while (r < rawLines.Length) {
				ThrowIfContainsInvalidChars(rawLines[r]);
				rawLine = rawLines[r] + ' ';
				if (rawLines[r + 1] != string.Empty) throw new Exception("Missing raw line break.");

				string constructedLine = string.Empty;
				while (rawLine != constructedLine) {
					if (!rawLine.StartsWith(constructedLine)) throw new Exception("Mismatched pulp line.");

					pulpLine = pulpLines[p];

					sb.Append('`');
					sb.Append(pulpLine);
					sb.Append("`, ");

					constructedLine += pulpLine + ' ';
					if (pulpLines[p + 2] != string.Empty) throw new Exception("Missing pulp line break.");

					p += 3;
				}

				r += 2;
			}
		} catch (Exception e) {
			throw new Exception($"Parsing error at raw line {r} and pulp line {p}.", e) {
				Data = {
					["rawLine"] = rawLine,
					["pulpLine"] = pulpLine
				}
			};
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