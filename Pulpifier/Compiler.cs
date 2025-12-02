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
		sb.Append("<div id='app'></div>");
		sb.Append("""
		<script>
		const app = document.getElementById('app');
		const params = new URLSearchParams(window.location.search);
		let pos = Number(params.get("p"));
		if (Number.isNaN(pos)) pos = 0;
		pos = Math.floor(pos / 3);
		function buildPulp(i) {
		  if (i < 0 || i >= htmlArr.length) {
		    return `<div id='pulp'></div>`;
		  }
		  return `<div id='pulp' style='background-image: url("images/b-` + backgrounds[backgroundIds[i]] + `.webp")'>` + imageHtmls[i] + "<div id='foot'><div></div><div id='text'>" + htmlArr[i] + "</div><div></div></div></div>";
		}
		function appendPulp(i) {
		  app.innerHTML += buildPulp(i);
		}
		function prependPulp(i) {
		  app.innerHTML = buildPulp(i) + app.innerHTML;
		}
		function nextPulp() {
		  if (pos + 1 < htmlArr.length) {
		    app.removeChild(app.firstChild);
		    appendPulp(++pos + 1);
		  }
		}
		function prevPulp() {
		  if (pos - 1 >= 0) {
		    prependPulp(--pos - 1);
		    app.removeChild(app.lastChild);
		  }
		}
		document.addEventListener("keydown", function (e) {
		  if (e.key === " " || e.key === "Spacebar" || e.key === "ArrowRight") {
		    nextPulp();
		  } else if (e.key === "ArrowLeft") {
		    prevPulp();
		  }
		});
		document.addEventListener("click", function (e) {
		  if (e.target.tagName === "IMG" || (e.target.tagName === "DIV" && e.target.id === "pulp")) {
		    nextPulp();
		  }
		});
		document.addEventListener("wheel", function (e) {
		  if (e.deltaY > 0) {
		    nextPulp();
		  } else {
		    prevPulp();
		  }
		});
		window.addEventListener("load", e => {
		  appendPulp(pos - 1);
		  appendPulp(pos);
		  appendPulp(pos + 1);
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
		  overflow: hidden;
		}
		#pulp {
		  width: 100vw;
		  height: 100vh;
		  position: relative;
		  background-position: center;
		  background-repeat: no-repeat;
		  background-size: cover;
		  background-color: #f0ddb6;
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
		#text > h1, #text > h2, #text > h3, #text > h4, #text > h5, #text > h6 {
		  text-align: center;
		  margin: 0.25em 0px;
		}
		#text > hr {
		  margin: 1em 0px;
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
		Dictionary<string, string> characterExtras = new();
		List<string> backgrounds = new();
		List<string> imageHtmls = new();
		List<int> backgroundIds = new();
		imageFiles = new();
		string[] activeCharacters = [];
		string activeSpeaker = "";
		string activeThinker = "";
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
					if (cleanPulpLine == "") {
						if (pulpLine == "") throw new Exception("Should only have empty lines if there are editor's notes.");
					} else {
						if (constructedLine.Count(c => c == '"') % 2 == 1 && !cleanPulpLine.StartsWith('"')) throw new Exception("Pulp line should continue ongoing quote.");
						string constructionPulpLine = cleanPulpLine;
						if (constructionPulpLine.StartsWith('"') && rawLine[constructedLine.Length] != '"') {
							constructionPulpLine = constructionPulpLine[1..];
						}
						if (constructionPulpLine.EndsWith('"') && rawLine[constructedLine.Length + constructionPulpLine.Length - 1] != '"') {
							constructionPulpLine = constructionPulpLine[..^1];
						}
						constructedLine += constructionPulpLine + ' ';
					}

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
							case 'x':
								SetCharacterAttribute(key, value, characterNames, characterExtras);
								break;
							case 's':
								if (value != "" && !characterNames.ContainsKey(value)) throw new Exception("Missing character name for speaker.");
								activeSpeaker = value;
								break;
							case 't':
								if (value != "" && !characterNames.ContainsKey(value)) throw new Exception("Missing character name for thinker.");
								activeThinker = value;
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
							string file = GetCharacterFile(name, characterAges, characterExpressions, characterExtras, activeSpeaker, activeThinker);
							imageFiles.TryAdd(file, p);
							images += $"<img src='{directory}{file}.webp' class='p-{i+1}/{denominator}' />";
						}
					}
					imageHtmls.Add(images);

					List<string> htmlParts = new List<string>();
					string[] parts = Regex.Split(pulpLine, @"(<e>(.*?)</e>)", RegexOptions.Singleline);
					bool editorLine = false;
					for (int i = 0; i < parts.Length; i++) {
						string part = parts[i];
						if (editorLine) {
							editorLine = false;
							part = Regex.Replace(part, @"<book>(.*?)(\|.*?)?</book>", m => {
								string book = m.Groups[1].Value;
								string text = m.Groups[2].Value.Trim('|');
								if (text == "") text = book;
								string url = "https://www.goodreads.com/search?q=" + string.Join("+", book.ToLowerInvariant());
								return $"<a href='{url}'>{text}</a>";
							}, RegexOptions.Singleline);
							htmlParts.Add($"<p class='e'><b>Editor's Note:</b> {part}</p>");
						} else if (part.StartsWith("<e>")) {
							editorLine = true;
						} else {
							part = Regex.Replace(part, @"^—+$", "<hr>");
							part = Regex.Replace(part, @"(\S)—", "$1&#8288;—");
							part = Regex.Replace(part, @"—(\S)", "—&#8288;$1");

							part = Regex.Replace(part, @"\*\*\*(.*?)\*\*\*", "<b><i>$1</i></b>");
							part = Regex.Replace(part, @"\*\*(.*?)\*\*", "<b>$1</b>");
							part = Regex.Replace(part, @"\*(.*?)\*", "<i>$1</i>");

							part = Regex.Replace(part, @"^(#{1,6})\s+(.*)$",m => {
								int level = m.Groups[1].Value.Length;
								string text = m.Groups[2].Value;
								return $"<h{level}>{text}</h{level}>";
							}, RegexOptions.Singleline);
							htmlParts.Add(part);
						}
					}
					string htmlLine = string.Concat(htmlParts);

					sb.Append('`');
					if (activeSpeaker != "" && activeThinker != "") throw new Exception("Can't have both active speaker and active thinker.");
					if (activeSpeaker != "" || activeThinker != "") {
						string active = activeSpeaker != "" ? activeSpeaker : activeThinker;
						sb.Append($"<b class='speaker'>{characterNames[active]}{(activeThinker != "" ? " (Thinking)" : "")}</b>");

						string file = GetCharacterFile(active, characterAges, characterExpressions, characterExtras, activeSpeaker, activeThinker);
						imageFiles.TryAdd(file, p);

						if (!activeCharacters.Contains(active)) {
							sb.Append($"<img src='{directory}{file}.webp' class='speaker-img' />");
						}
					} else if (cleanPulpLine.Contains('"')) {
						//Console.WriteLine(p);
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

	private static string GetCharacterFile(string name,  Dictionary<string, string> ages, Dictionary<string, string> expressions, Dictionary<string, string> extras, string speaker, string thinker) {
		string file = "c-" + name;
		if (ages.TryGetValue(name, out string age)) file += "-a" + age;
		if (expressions.TryGetValue(name, out string expression) && expression != "") {
			file += "-e" + expression;
		} else if (name == thinker) {
			file += "-et";
		}
		if (extras.TryGetValue(name, out string extra)) file += "-x" + extra;
		if (name == speaker) file += "-s";
		return file;
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