using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Pulp.Pulpifier;

public class ImageMetadata(int pulpLine) {
	public int PulpLine { get; } = pulpLine;
	public int? ForegroundPulpLine { get; set; }
}

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

	public static string BuildHtml(string rawText, string pulpText, out Dictionary<string, ImageMetadata> imageFiles) {
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("<div id='app'></div>");
		sb.AppendLine("<style>");
		sb.Append(ReadResource("style.css"));
		sb.Append("</style>");
		sb.Append("<link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">");
		sb.Append("<link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin>");
		sb.Append("<link href=\"https://fonts.googleapis.com/css2?family=Noto+Serif:ital,wght@0,100..900;1,100..900&display=swap\" rel=\"stylesheet\">");
		sb.Append("<script>let i = 0; const htmlArr = [");

		StringBuilder styleBuilder = new StringBuilder();

		string[] rawLines = rawText.Split('\n');
		string[] pulpLines = pulpText.Split('\n');

		Dictionary<string, string> characterNames = new() { { "author", "" } };
		Dictionary<string, string> characterExpressions = new();
		Dictionary<string, int> characterExpressionCounters = new();
		Dictionary<string, string> characterAges = new();
		Dictionary<string, string[]> characterExtras = new();
		Dictionary<string, string> characterFilters = new();
		HashSet<string> characterZooms = new();
		List<string> backgrounds = new();
		List<string> imageHtmls = new();
		List<int> backgroundIds = new();
		List<string> speakers = new();
		imageFiles = new();
		List<string> headers = new();
		string[] activeCharacters = [];
		Dictionary<string, string[]> backgroundModifiers = new();
		Dictionary<string, string> backgroundFilters = new();
		string activeSpeaker = "";
		string activeThinker = "";
		string activeBackground = "";
		string activeObject = "";
		Tuple<int, int> viewScale = null;

		int r = 0, p = 0;
		try {
			while (r < rawLines.Length) {
				ThrowIfContainsInvalidChars(rawLines[r]);
				string rawLine = rawLines[r].Replace("\uFEFF", string.Empty).Replace("\u200A…", "…").Replace("\u00a0", " ").Replace("“\u200a’", "“’").Replace("’\u200a”", "’”") + ' ';
				if (rawLine == " ") throw new Exception("Empty raw line.");
				if (rawLines[r + 1] != string.Empty) throw new Exception("Missing raw line break.");

				string constructedLine = string.Empty;
				while (rawLine != constructedLine || (r + 2 == rawLines.Length && p < pulpLines.Length)) {
					if (p >= pulpLines.Length - 2) throw new Exception("Early pulp end.");

					if (!rawLine.StartsWith(constructedLine, StringComparison.Ordinal)) {
						int diff = -1;
						for (int i = 0; diff == - 1 && i < rawLine.Length && i < constructedLine.Length; i++) {
							if (rawLine[i] != constructedLine[i]) {
								diff = i;
							}
						}
						throw new Exception($"Mismatched pulp line.\nBook: {rawLine}\n\nPulp: {constructedLine}\n\nDiff Char: {diff}\n");
					}

					if (constructedLine != string.Empty && !(Regex.IsMatch(constructedLine, "[\\.!?:;—…]['’\\\"”\\*\\)\\]]* $") || (constructedLine.EndsWith(", ", StringComparison.Ordinal) && IsOpenQuote(rawLine[constructedLine.Length])))) {
						throw new Exception("Line break in the middle of a sentence.");
					}

					string pulpLine = pulpLines[p].Replace("\uFEFF", string.Empty).Replace("\u200A…", "…").Replace("\u00a0", " ").Replace("“\u200a’", "“’").Replace("’\u200a”", "’”");

					string cleanPulpLine = Regex.Replace(pulpLine, "<e>.*?</e>", "");
					if (cleanPulpLine == "") {
						if (pulpLine == "") throw new Exception("Should only have empty lines if there are editor's notes.");
					} else {
						if (constructedLine.Count(c => c == '"') % 2 == 1 && !cleanPulpLine.StartsWith('"')) throw new Exception("Pulp line should continue ongoing quote.");
						string constructionPulpLine = cleanPulpLine;
						if ((constructionPulpLine.StartsWith('"') && rawLine[constructedLine.Length] != '"') || (constructionPulpLine.StartsWith('“') && rawLine[constructedLine.Length] != '“')) {
							constructionPulpLine = constructionPulpLine[1..];
						}
						if ((constructionPulpLine.EndsWith('"') && rawLine[constructedLine.Length + constructionPulpLine.Length - 1] != '"') || (constructionPulpLine.EndsWith('”') && rawLine[constructedLine.Length + constructionPulpLine.Length - 1] != '”')) {
							constructionPulpLine = constructionPulpLine[..^1];
						}
						constructedLine += constructionPulpLine + ' ';
					}

					if (cleanPulpLine.Count(c => c == '"') % 2 == 1) throw new Exception("Unmatched quotes in pulp line.");
					if (cleanPulpLine.Count(c => c == '“') != cleanPulpLine.Count(c => c == '”')) throw new Exception("Unmatched quotes in pulp line.");

					if (pulpLines[p + 2] != string.Empty) throw new Exception("Missing pulp line break.");

					string[] metadata = pulpLines[p + 1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
					foreach (string data in metadata) {
						string[] kvp = data.Split('=');
						if (kvp.Length < 2) throw new Exception("Missing equal sign.");
						if (kvp.Length > 2) throw new Exception("Extra equal sign.");
						string key = kvp[0];
						string value = kvp[1];

						switch (key[0]) {
							case 'n':
								string name = key.Split(':')[1];
								if (string.Equals(name, "none", StringComparison.OrdinalIgnoreCase)) throw new Exception("Forbidden name.");
								characterNames[name] = value;
								break;
							case 'c':
								ThrowIfBadKey(key);
								if (value == "") {
									activeCharacters = [];
								} else {
									string[] characters = value.Split(',');
									if (characters.Length > 5) throw new Exception("Unsupported number of characters");
									if (characters.Any(c => c != "" && !characterNames.ContainsKey(c.TrimStart('!')))) throw new Exception("Missing character name.");
									if (characters.Any(c => c.TrimStart('!') == "author")) throw new Exception("Author can't be non-thinker.");
									activeCharacters = characters;
								}
								break;
							case 'o':
								ThrowIfBadKey(key);
								activeObject = value;
								break;
							case 'b':
								ThrowIfBadKey(key);
								activeBackground = value;
								backgroundModifiers.TryAdd(value, []);
								break;
							case 'r':
								ThrowIfBadKey(key);
								activeBackground = value;
								backgroundModifiers.TryAdd(value, []);
								characterExtras.Clear();
								characterExpressions.Clear();
								characterExpressionCounters.Clear();
								characterFilters.Clear();
								backgroundFilters.Clear();
								activeSpeaker = "";
								activeThinker = "";
								activeObject = "";
								activeCharacters = [];
								foreach (string bkey in backgroundModifiers.Keys) {
									backgroundModifiers[bkey] = [];
								}
								viewScale = null;
								break;
							case 'm':
								string bname = key.Split(':')[1];
								string[] bvalues = value == "" ? [] : value.Split(',');
								bvalues.Sort();
								if (!backgroundModifiers.ContainsKey(bname)) throw new Exception("Can't set modifiers on non-existent background.");
								backgroundModifiers[bname] = bvalues;
								break;
							case 'e':
								if (!Regex.IsMatch(value, "^[a-z]*$")) throw new Exception("Unexpected expression name.");
								string keyname = key.Split(':')[1];
								characterExpressions.TryGetValue(keyname, out string? prevValue);
								SetCharacterAttribute(key, value, characterNames, characterExpressions);
								if (prevValue != value && !(IsNeutralExpression(prevValue) && IsNeutralExpression(value))) characterExpressionCounters.Remove(keyname);
								break;
							case 'a':
								SetCharacterAttribute(key, value, characterNames, characterAges);
								characterExpressionCounters.Remove(key.Split(':')[1]);
								break;
							case 'x':
								string[] extras = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
								extras.Sort();
								SetCharacterAttribute(key, extras, characterNames, characterExtras);
								characterExpressionCounters.Remove(key.Split(':')[1]);
								break;
							case 's':
								ThrowIfBadKey(key);
								if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)) value = "";
								if (value != "" && !characterNames.ContainsKey(value)) throw new Exception("Missing character name for speaker.");
								activeSpeaker = value;
								break;
							case 't':
								ThrowIfBadKey(key);
								if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase)) value = "";
								if (value != "" && !characterNames.ContainsKey(value)) throw new Exception("Missing character name for thinker.");
								activeThinker = value;
								break;
							case 'z':
								string zn = key.Split(':')[1];
								if (!characterNames.ContainsKey(zn.Split('-')[0])) throw new Exception($"Missing character name \"{zn}\" for zoom.");
								if (!characterZooms.Add(zn)) throw new Exception("Can't add already added zoom.");
								string[] zvals = value.Split(',');

								int zzoom = int.Parse(zvals[0]);
								int ch = zzoom * 13 / 10;
								styleBuilder.AppendLine($".characters > img[src^='images/c-{zn}.'], .characters > img[src^='images/c-{zn}-'] {{ height: {ch}% {(zn.Contains("-a") ? "!important" : "")}; }}");
								int sh = zzoom * 2 / 5;
								string zcss = $"background-size: {sh}em {(zn.Contains("-a") ? "!important" : "")}";
								if (zvals.Length > 1) {
									int zx = int.Parse(zvals[1]);
									int zy = int.Parse(zvals[2]);
									zcss += $"; background-position: {zx}% {zy}% {(zn.Contains("-a") ? "!important" : "")}";
								}

								styleBuilder.AppendLine($".speaker-back[style*='background-image: url(images/c-{zn}.'], .speaker-back[style*='background-image: url(images/c-{zn}-'] {{ {zcss}; }}");
								break;
							case 'f':
								if (key.StartsWith("f:c:")) {
									SetCharacterAttribute(key.Substring(2), value, characterNames, characterFilters);
								} else if (key.StartsWith("f:b:")) {
									SetCharacterAttribute(key.Substring(2), value, backgroundModifiers, backgroundFilters);
								} else {
									throw new Exception($"Invalid filter key \"{key}\".");
								}
								break;
							case 'v':
								if (value == "") {
									viewScale = null;
								} else {
									string[] viewWxH = value.Split(',');
									viewScale = new Tuple<int, int>(int.Parse(viewWxH[0]), int.Parse(viewWxH[1]));
								}
								break;
							case 'h':
								if (p != 0) throw new Exception("Header settings can only be declared on the first pulp line.");
								if (value == "no-speaker-counter") _sSpeakerCounterEnabled = false;
								break;
							default: throw new Exception($"Unrecognized key: '{key}'.");
						}
					}

					string backgroundFullName = activeBackground;
					if (backgroundModifiers.TryGetValue(activeBackground, out string[] bmods) && bmods.Length > 0) {
						backgroundFullName += "-mod-";
						backgroundFullName += string.Join('-', bmods);
					}
					imageFiles.TryAdd("b-" + backgroundFullName, new ImageMetadata(p));
					if (activeCharacters.Length > 0) imageFiles["b-" + backgroundFullName].ForegroundPulpLine = p;
					if (backgroundFilters.TryGetValue(activeBackground, out string bfilter) && bfilter != "") {
						backgroundFullName += ";" +  bfilter;
					}
					int bIndex = backgrounds.IndexOf(backgroundFullName);
					if (bIndex == -1) {
						bIndex = backgrounds.Count;
						backgrounds.Add(backgroundFullName);
					}
					backgroundIds.Add(bIndex);

					foreach (string counterKey in characterExpressionCounters.Keys) {
						if (counterKey != activeSpeaker && activeCharacters.All(ac => ac.Trim('!') != counterKey)) {
							 characterExpressionCounters.Remove(counterKey);
						}
					}

					if (activeSpeaker != "") {
						characterExpressionCounters.TryGetValue(activeSpeaker, out int expressionCounter);
						characterExpressionCounters[activeSpeaker] = expressionCounter + 1;
					}

					string directory = "images/";
					string images = "";
					if (activeCharacters.Length > 0) {
						if (viewScale == null) {
							images = "<div class='characters'>";
						} else {
							images = $"<div class='characters' style='width:{viewScale.Item1}%; height:{viewScale.Item2}%;'>";
						}

						int denominator = activeCharacters.Length + 1;
						for (int i = 0; i < activeCharacters.Length; i++) {
							string name = activeCharacters[i];
							if (name != "") {
								bool flip = false;
								if (name[0] == '!') {
									flip = true;
									name = name.Substring(1);
								}

								if (name.Contains('!')) throw new Exception("Name should not contain exclamations.");
								string file = GetCharacterFile(name, characterAges, characterExpressions, characterExpressionCounters, characterExtras, activeSpeaker, activeThinker);
								imageFiles.TryAdd(file, new ImageMetadata(p));
								images += $"<img src='{directory}{file}.webp' class='{(flip ? "f " : "")}p-{i + 1}/{denominator}' ";
								if (characterFilters.TryGetValue(name, out string filter) && name != "") {
									images += $"style='filter:{filter}' ";
								}

								images += "/>";
							}
						}

						images += "</div>";
					}

					if (activeObject != "") {
						string file = "o-" + activeObject;
						imageFiles.TryAdd(file, new ImageMetadata(p));
						images += $"<img src='{directory}{file}.webp' class='c' />";
					}
					imageHtmls.Add(images);

					List<string> htmlParts = new List<string>();
					string[] parts = Regex.Split(pulpLine, @"(<e>(.*?)</e>)", RegexOptions.Singleline);
					bool editorLine = false;
					for (int i = 0; i < parts.Length; i++) {
						string part = parts[i];
						if (editorLine) {
							editorLine = false;
							part = BookTag.FormatText(part);
							htmlParts.Add($"<p class='e'><span class='upper'>Editor's Note:</span> {part}</p>");
						} else if (part.StartsWith("<e>")) {
							editorLine = true;
						} else {
							part = Regex.Replace(part, @"^—+$", "<hr>");

							part = Regex.Replace(part, @"\*\*\*(.*?)\*\*\*", "<i class='upper'>$1</i>");
							part = Regex.Replace(part, @"\*\*(.*?)\*\*", "<span class='upper'>$1</span>");
							part = Regex.Replace(part, @"\*(.*?)\*", "<i>$1</i>");
							part = Regex.Replace(part, @"“(.*?)”", "<span class='d'>“$1”</span>");

							part = Regex.Replace(part, @"^(#{1,6})\s+(.*)$",m => {
								int level = Math.Max(1, Math.Min(6, m.Groups[1].Value.Length - 1));
								string text = m.Groups[2].Value;
								if (text.Contains('|')) throw new Exception("Header should not have a pipe.");
								headers.Add($"{p / 3}|{level}|{text}");
								return $"<h{level} class='upper'>{text}</h{level}>";
							}, RegexOptions.Singleline);
							htmlParts.Add(part);
						}
					}
					string htmlLine = string.Concat(htmlParts);
					htmlLine = Regex.Replace(htmlLine, @"(\S)—", "$1&#8288;—");
					htmlLine = Regex.Replace(htmlLine, @"—(\S)", "—&#8288;$1");

					sb.Append('`');
					if (activeSpeaker != "" && activeThinker != "") throw new Exception("Can't have both active speaker and active thinker.");
					if (activeSpeaker != "" && !(cleanPulpLine.Contains('"') || (cleanPulpLine.Contains('“') && cleanPulpLine.Contains('”')))) throw new Exception("Active speaker on a quote-less line.");
					if (activeSpeaker != "" || activeThinker != "") {
						string active = activeSpeaker != "" ? activeSpeaker : activeThinker;
						string activeName = characterNames[active];
						if (activeName != "") {
							sb.Append($"<b class='speaker'>{characterNames[active]}{(activeThinker != "" ? " (Thinking)" : "")}</b>");
						}

						string file = GetCharacterFile(active, characterAges, characterExpressions, characterExpressionCounters, characterExtras, activeSpeaker, activeThinker);
						imageFiles.TryAdd(file, new ImageMetadata(p));

						if (activeCharacters.All(c => c.TrimStart('!') != active)) {
							speakers.Add(file + ".webp");
						} else {
							speakers.Add("");
						}
					} else {
						speakers.Add("");
						//if (cleanPulpLine.Contains('"')) {
						//Console.WriteLine(p);
						//}
					}
					sb.Append("<div>");
					sb.Append(htmlLine);
					sb.Append("</div>");
					sb.Append("`, ");

					p += 3;
				}

				r += 2;
			}
			if (p != pulpLines.Length) throw new Exception("Extra pulp lines.");
		} catch (Exception e) {
			string error = $"Parsing error at raw line {r} and pulp line {p}.\n";
			if (rawLines.Length > r) error += '\n' + rawLines[r] + '\n';
			if (pulpLines.Length > p) error += '\n' + pulpLines[p] + '\n';
			if (pulpLines.Length > p + 1) error += '\n' + pulpLines[p + 1] + '\n';
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
		sb.Append("speakers=['");
		sb.Append(string.Join("','", speakers));
		sb.Append("'];");
		sb.Append("headers=[`");
		sb.Append(string.Join("`,`", headers));
		sb.Append("`];");
		sb.Append("</script><script>");
		sb.Append(ReadResource("script.js"));
		sb.Append("</script><style>");
		sb.Append(styleBuilder);
		sb.Append("</style></div>");
		return sb.ToString();
	}

	private static readonly string[] ExpressionVariationArr = ["", "2", "", "2", "3", "", "3", "2"];
	private static bool _sSpeakerCounterEnabled = true;

	private static string GetCharacterFile(string name,  Dictionary<string, string> ages, Dictionary<string, string> expressions, Dictionary<string, int> characterExpressionCounters, Dictionary<string, string[]> extras, string speaker, string thinker) {
		string file = "c-" + name;
		if (ages.TryGetValue(name, out string age) && age != string.Empty) file += "-a" + age;
		if (extras.TryGetValue(name, out string[] xarr)) {
			foreach (string extra in xarr) {
				file += "-x" + extra;
			}
		}
		if (expressions.TryGetValue(name, out string? expression) && !IsNeutralExpression(expression)) {
			file += "-e" + expression;
		}
		if (name == speaker) {
			file += "-s";
			int counterIndex = (characterExpressionCounters[name] - 1) % ExpressionVariationArr.Length;
			if (_sSpeakerCounterEnabled) file += ExpressionVariationArr[counterIndex];
		}
		return file;
	}

	private static bool IsNeutralExpression(string? expression) {
		return string.IsNullOrEmpty(expression) || expression == "neutral";
	}

	private static void SetCharacterAttribute<T, T2>(string key, T value, Dictionary<string, T2> characterNames, Dictionary<string, T> characterAttributes) {
		string name = key.Split(':')[1];
		if (!characterNames.ContainsKey(name)) throw new Exception($"Missing character name \"{name}\" for expression.");
		characterAttributes[name] = value;
	}

	private static bool IsOpenQuote(char c) {
		return c == '"' || c == '“';
	}

	private static readonly char[] InvalidChars = ['`'];
	private static void ThrowIfContainsInvalidChars(string line) {
		if (line.Any(char.IsControl)) throw new Exception("Contains control char.");
		if (line.ContainsAny(InvalidChars)) throw new Exception("Contains invalid char.");
		if (line.Contains("...") || line.Contains(". . .") || line.Contains("--")) throw new Exception("Contains invalid sequence.");
		if (line != line.Trim()) throw new Exception("Contains leading/trailing whitespace.");
	}

	private static void ThrowIfBadKey(string key) {
		if (key.Length != 1) throw new Exception($"Key \"{key}\" should be single letter.");
	}

	private static string ReadResource(string name)
	{
		Assembly asm = Assembly.GetExecutingAssembly();
		using Stream stream = asm.GetManifestResourceStream("Pulp.Pulpifier." + name);
		using StreamReader reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}