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

	public static string BuildHtml(string rawText, string pulpText) => BuildHtml(rawText, pulpText, out _);

	public static string BuildHtml(string rawText, string pulpText, out Dictionary<string, int> imageFiles) {
		StringBuilder sb = new StringBuilder();
		sb.Append("<div id='app'><div id='pulp'></div></div>");
		sb.Append("""
		<script>
		const app = document.getElementById('app');
		function buildPulp(i) {
		  return `<div id='pulp' style='background-image: url("images/b-` + backgrounds[backgroundIds[i]] + `.webp")'>` + imageHtmls[i] + "<div id='foot'><div></div><div id='text'>" + htmlArr[i] + "</div><div></div></div></div>";
		}
		function appendPulp(i) {
		  app.innerHTML += buildPulp(i);
		}
		function prependPulp(i) {
		  app.innerHTML = buildPulp(i) + app.innerHTML;
		}
		document.addEventListener("keydown", function (e) {
		  if (e.key === " " || e.key === "Spacebar" || e.key === "ArrowRight") {
		    app.removeChild(app.firstChild);
		    appendPulp(++i + 1);
		  } else if (e.key === "ArrowLeft") {
		    prependPulp(--i - 1);
		    app.removeChild(app.lastChild);
		  }
		});
		document.addEventListener("click", function (e) {
		  app.removeChild(app.firstChild);
		  appendPulp(++i + 1);
		});
		window.addEventListener("load", e => {
		  appendPulp(0);
		  appendPulp(1);
		  window.scrollBy({ top: window.innerHeight });
		});
		</script>
		""");

		sb.Append("""
		<style>
		html, body {
		  margin: 0px;
		  padding: 0px;
		}
		body {
		  background-color: black;
		}
		#pulp {
		  width: 100vw;
		  height: 100vh;
		  position: relative;
		  background-position: center;
		  background-repeat: no-repeat;
		  background-size: cover;
		}
		#foot {
		  width: 100%;
		  min-height: 15em;
		  display: flex;
		  position: absolute;
		  bottom: 0px;
		  justify-content: center;
		}
		#text {
		  width: 40em;
		  font-family: sans-serif;
		  font-size: 2em;
		  position: relative;
		  color: black;
		  background-color: #f8efd4f0;
		}
		#foot > div:first-child {
		  flex-grow: 1;
		  background: linear-gradient(to right, #f8efd445, #f8efd4f0);
		}
		#foot > div:last-child {
		  flex-grow: 1;
		  background: linear-gradient(to left, #f8efd445, #f8efd4f0);
		}
		b.speaker {
		  position: absolute;
		  top: -2em;
		  background-color: #f8efd4f0;
		  padding: 0.25em;
		  min-width: 2em;
		  text-align: center;
		}
		img.speaker-img {
		  position: absolute;
		  transform: translateX(-50%);
		  left: -130px;
		  height: 120vh;
		}
		p.e {
		  margin: 0.25em;
		  font-size: 0.8em;
		}
		#pulp > img {
		  position: absolute;
		  transform: translateX(-50%);
		  bottom: -15vh;
		  height: 100vh;
		}
		img.p-1\/6 { left: calc(100% * 1/6); }
		img.p-2\/6, img.p-1\/3 { left: calc(100% * 2/6); }
		img.p-3\/6, img.p-2\/4, img.p-1\/2 { left: 50%; }
		img.p-4\/6, img.p-2\/3 { left: calc(100% * 4/6); }
		img.p-5\/6 { left: calc(100% * 5/6); }
		img.p-1\/5 { left: 20%; }
		img.p-2\/5 { left: 40%; }
		img.p-3\/5 { left: 60%; }
		img.p-4\/5 { left: 80%; }
		img.p-1\/4 { left: 25%; }
		img.p-3\/4 { left: 75%; }
		</style>
		""");

		sb.Append("<script>let i = 0; const htmlArr = [");

		string[] rawLines = rawText.Split('\n');
		string[] pulpLines = pulpText.Split('\n');

		Dictionary<string, string> characterNames = new();
		Dictionary<string, string> characterExpressions = new();
		Dictionary<string, string> characterAges = new();
		List<string> backgrounds = new();
		List<string> imageHtmls = new();
		List<int> backgroundIds = new();
		imageFiles = new();
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
					if (!rawLine.StartsWith(constructedLine)) {
						throw new Exception($"Mismatched pulp line.\nBook: {rawLine}\nPulp: {constructedLine}");
					}

					string pulpLine = pulpLines[p];

					string cleanPulpLine = Regex.Replace(pulpLine, "<e>.*?</e>", "");
					if (constructedLine.Count(c => c == '"') % 2 == 1 && !cleanPulpLine.StartsWith('"')) throw new Exception("Pulp line should continue ongoing quote.");

					string constructionPulpLine = cleanPulpLine;
					if (constructionPulpLine.StartsWith('"') && rawLine[constructedLine.Length] != '"') {
						constructionPulpLine = constructionPulpLine[1..];
					}
					if (constructionPulpLine.EndsWith('"') && rawLine[constructedLine.Length + constructionPulpLine.Length - 1] != '"') {
						constructionPulpLine = constructionPulpLine[..^1];
					}
					constructedLine += constructionPulpLine + ' ';

					if (cleanPulpLine.Count(c => c == '"') % 2 == 1) throw new Exception("Unmatched quotes in pulp line.");

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
									if (characters.Length > 5) throw new Exception("Unsupported number of characters");
									if (characters.Any(c => c != "" && !characterNames.ContainsKey(c))) throw new Exception("Missing character name.");
									activeCharacters = characters;
								}
								break;
							case 'b':
								activeBackground = value;
								if (imageFiles.TryAdd("b-" + activeBackground, p)) {
									backgrounds.Add(activeBackground);
								}
								break;
							case 'e':
								SetCharacterAttribute(key, value, characterNames, characterExpressions);
								break;
							case 'a':
								SetCharacterAttribute(key, value, characterNames, characterAges);
								break;
							case 's':
								if (value != "" && !characterNames.ContainsKey(value)) throw new Exception("Missing character name for speaker.");
								activeSpeaker = value;
								break;
							default: throw new Exception($"Unrecognized key: '{key}'.");
						}
					}

					backgroundIds.Add(backgrounds.IndexOf(activeBackground));

					string directory = "images/";
					string images = "";
					int denominator = activeCharacters.Length + 1;
					for (int i = 0; i < activeCharacters.Length; i++) {
						string name = activeCharacters[i];
						if (name != "") {
							string file = "c-" + name;
							if (characterAges.TryGetValue(name, out string age)) file += "-a" + age;
							if (characterExpressions.TryGetValue(name, out string expression) && expression != "") file += "-e" + expression;
							if (activeSpeaker == name) file += "-s";
							imageFiles.TryAdd(file, p);
							images += $"<img src='{directory}{file}.webp' class='p-{i+1}/{denominator}' />";
						}
					}
					imageHtmls.Add(images);

					if (activeSpeaker != "") {
						string file = "c-" + activeSpeaker;
						if (characterAges.TryGetValue(activeSpeaker, out string age)) file += "-a" + age;
						if (characterExpressions.TryGetValue(activeSpeaker, out string expression) && expression != "") file += "-e" + expression;
						file += "-s";
						imageFiles.TryAdd(file, p);
					}

					string htmlLine = Regex.Replace(pulpLine, @"<e>(.*?)</e>", "<p class='e'><b>Editor's Note:</b> $1</p>");

					sb.Append('`');
					if (activeSpeaker != "") {
						sb.Append($"<b class='speaker'>{characterNames[activeSpeaker]}</b>");

						string file = "c-" + activeSpeaker;
						if (characterAges.TryGetValue(activeSpeaker, out string age)) file += "-a" + age;
						if (characterExpressions.TryGetValue(activeSpeaker, out string expression) && expression != "") file += "-e" + expression;
						file += "-s";
						imageFiles.TryAdd(file, p);

						if (!activeCharacters.Contains(activeSpeaker)) {
							sb.Append($"<img src='{directory}{file}.webp' class='speaker-img' />");
						}
					}
					sb.Append(htmlLine);
					sb.Append("`, ");

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

		sb.Append("];");
		sb.Append("backgrounds=['");
		sb.Append(string.Join("','", backgrounds));
		sb.Append("'];");
		sb.Append("backgroundIds=[");
		sb.Append(string.Join(",", backgroundIds));
		sb.Append("];");
		sb.Append("imageHtmls=[`");
		sb.Append(string.Join("`,`", imageHtmls));
		sb.Append("`];");
		sb.Append("</script></div>");
		return sb.ToString();
	}

	private static void SetCharacterAttribute(string key, string value, Dictionary<string, string> characterNames, Dictionary<string, string> characterAttributes) {
		string name = key.Split(':')[1];
		if (!characterNames.ContainsKey(name)) throw new Exception("Missing character name for expression.");
		characterAttributes[name] = value;
	}

	private static readonly char[] InvalidChars = ['`', '“', '”', '‘', '’'];
	private static void ThrowIfContainsInvalidChars(string line) {
		if (line.Any(char.IsControl)) throw new Exception("Contains control char.");
		if (line.ContainsAny(InvalidChars)) throw new Exception("Contains invalid char.");
		if (line.Contains("...") || line.Contains("--")) throw new Exception("Contains invalid sequence.");
		if (line != line.Trim()) throw new Exception("Contains leading/trailing whitespace.");
	}
}