using System.Reflection;
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
		sb.AppendLine("<div id='app'></div><script>");
		sb.Append(ReadResource("script.js"));
		sb.AppendLine("</script><style>");
		sb.Append(ReadResource("style.css"));
		sb.Append("</style>");
		sb.Append("<script>let i = 0; const htmlArr = [");

		StringBuilder styleBuilder = new StringBuilder();
		
		string[] rawLines = rawText.Split('\n');
		string[] pulpLines = pulpText.Split('\n');

		Dictionary<string, string> characterNames = new();
		Dictionary<string, string> characterExpressions = new();
		Dictionary<string, string> characterAges = new();
		Dictionary<string, string[]> characterExtras = new();
		Dictionary<string, string> characterFilters = new();
		List<string> backgrounds = new();
		List<string> imageHtmls = new();
		List<int> backgroundIds = new();
		List<string> speakers = new();
		imageFiles = new();
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
								ThrowIfBadKey(key);
								if (value == "") {
									activeCharacters = [];
								} else {
									string[] characters = value.Split(',');
									if (characters.Length > 5) throw new Exception("Unsupported number of characters");
									if (characters.Any(c => c != "" && !characterNames.ContainsKey(c.TrimStart('!')))) throw new Exception("Missing character name.");
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
								characterFilters.Clear();
								activeSpeaker = "";
								activeThinker = "";
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
								SetCharacterAttribute(key, value, characterNames, characterExpressions);
								break;
							case 'a':
								SetCharacterAttribute(key, value, characterNames, characterAges);
								break;
							case 'x':
								string[] extras = value.Split(',', StringSplitOptions.RemoveEmptyEntries);
								extras.Sort();
								SetCharacterAttribute(key, extras, characterNames, characterExtras);
								break;
							case 's':
								ThrowIfBadKey(key);
								if (value != "" && !characterNames.ContainsKey(value)) throw new Exception("Missing character name for speaker.");
								activeSpeaker = value;
								break;
							case 't':
								ThrowIfBadKey(key);
								if (value != "" && !characterNames.ContainsKey(value)) throw new Exception("Missing character name for thinker.");
								activeThinker = value;
								break;
							case 'z':
								string[][] zooms = value.Split(',').Select(z => z.Split(':')).ToArray();
								foreach (string[] zoom in zooms) {
									string zn = zoom[0];
									if (!characterNames.ContainsKey(zn.Split('-')[0])) throw new Exception($"Missing character name \"{zn}\" for zoom.");
									int ch = int.Parse(zoom[1]) * 13 / 10;
									styleBuilder.AppendLine($".characters > img[src^='images/c-{zn}.'], .characters > img[src^='images/c-{zn}-'] {{ height: {ch}% {(zn.Contains("-a") ? "!important" : "")}; }}");
									int sh = int.Parse(zoom[1]) * 2 / 5;
									styleBuilder.AppendLine($".speaker-back[style*='background-image: url(images/c-{zn}.'], .speaker-back[style*='background-image: url(images/c-{zn}-'] {{ background-size: {sh}em {(zn.Contains("-a") ? "!important" : "")}; }}");
								}
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
							default: throw new Exception($"Unrecognized key: '{key}'.");
						}
					}

					string backgroundFullName = activeBackground;
					if (backgroundModifiers.TryGetValue(activeBackground, out string[] bmods) && bmods.Length > 0) {
						backgroundFullName += "-mod-";
						backgroundFullName += string.Join('-', bmods);
					}
					imageFiles.TryAdd("b-" + backgroundFullName, p);
					if (backgroundFilters.TryGetValue(activeBackground, out string bfilter) && bfilter != "") {
						backgroundFullName += ";" +  bfilter;
					}
					int bIndex = backgrounds.IndexOf(backgroundFullName);
					if (bIndex == -1) {
						bIndex = backgrounds.Count;
						backgrounds.Add(backgroundFullName);
					}
					backgroundIds.Add(bIndex);

					string directory = "images/";
					string images = "<div class='characters'>";
					if (viewScale != null) {
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
							string file = GetCharacterFile(name, characterAges, characterExpressions, characterExtras, activeSpeaker, activeThinker);
							imageFiles.TryAdd(file, p);
							images += $"<img src='{directory}{file}.webp' class='{(flip ? "f ": "")}p-{i+1}/{denominator}' ";
							if (characterFilters.TryGetValue(name, out string filter) && name != "") {
								images += $"style='filter:{filter}' ";
							}
							images += "/>";
						}
					}
					images += "</div>";
					if (activeObject != "") {
						string file = "o-" + activeObject;
						imageFiles.TryAdd(file, p);
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
							part = Regex.Replace(part, @"<book>(.*?)(\|.*?)?</book>", m => {
								string book = m.Groups[1].Value;
								string text = m.Groups[2].Value.Trim('|');
								string url = "https://www.goodreads.com/search?q=" + Uri.EscapeDataString(book.ToLowerInvariant());
								return text == "" ? $"<i><a href='{url}'>{book}</a></i>" : $"<a href='{url}'>{text}</a>";
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
		sb.Append("speakers=['");
		sb.Append(string.Join("','", speakers));
		sb.Append("'];");
		sb.Append("</script><style>");
		sb.Append(styleBuilder);
		sb.Append("</style></div>");
		return sb.ToString();
	}

	private static string GetCharacterFile(string name,  Dictionary<string, string> ages, Dictionary<string, string> expressions, Dictionary<string, string[]> extras, string speaker, string thinker) {
		string file = "c-" + name;
		if (ages.TryGetValue(name, out string age)) file += "-a" + age;
		if (extras.TryGetValue(name, out string[] xarr)) {
			foreach (string extra in xarr) {
				file += "-x" + extra;
			}
		}
		if (expressions.TryGetValue(name, out string expression) && expression != "") {
			if (expression != "0") {
				file += "-e" + expression;
			}
		} else if (name == thinker) {
			file += "-et";
		}
		if (name == speaker) file += "-s";
		return file;
	}

	private static void SetCharacterAttribute<T, T2>(string key, T value, Dictionary<string, T2> characterNames, Dictionary<string, T> characterAttributes) {
		string name = key.Split(':')[1];
		if (!characterNames.ContainsKey(name)) throw new Exception($"Missing character name \"{name}\" for expression.");
		characterAttributes[name] = value;
	}

	private static readonly char[] InvalidChars = ['`', '“', '”', '‘', '’'];
	private static void ThrowIfContainsInvalidChars(string line) {
		if (line.Any(char.IsControl)) throw new Exception("Contains control char.");
		if (line.ContainsAny(InvalidChars)) throw new Exception("Contains invalid char.");
		if (line.Contains("...") || line.Contains("--")) throw new Exception("Contains invalid sequence.");
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