namespace Pulp.Pulpifier.Tests;

[TestClass]
public class CompilerTests {
	[TestMethod]
	public void Compiler_BuildHTML_ReturnsHtmlForMatching() {
		string rawText = "Foo. Bar.\n\nFizz. Buzz.\n";
		string pulpText = "Foo.\n\n\nBar.\n\n\nFizz. Buzz.";
		Assert.IsNotEmpty(Compiler.BuildHTML(rawText, pulpText));
	}
}