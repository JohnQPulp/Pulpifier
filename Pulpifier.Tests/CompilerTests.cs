namespace Pulp.Pulpifier.Tests;

[TestClass]
public class CompilerTests {
	[TestMethod]
	[DataRow("Foo\n", "Foo\n\n")]
	[DataRow("Foo Bar\n", "Foo Bar\n\n")]
	[DataRow("Foo\n\nFizz\n\nBuzz\n", "Foo\n\n\nFizz\n\n\nBuzz\n\n")]
	[DataRow("\"Foo\"\n", "\"Foo\"\n\n")]
	[DataRow("Foo. Fizz\n\nBuzz\n", "Foo.\n\n\nFizz\n\n\nBuzz\n\n")]
	public void Compiler_BuildHtml_GoodText(string rawText, string pulpText) {
		Compiler.BuildHtml(rawText, pulpText);
		Assert.IsTrue(Compiler.TryBuildHtml(rawText, pulpText, out string _));
	}

	[TestMethod]
	[DataRow("Foo`\n", "Foo`\n\n")]
	[DataRow("Foo\x0005\n", "Foo\x0005\n\n")]
	[DataRow("Foo\r\n", "Foo\n\n")]
	[DataRow("Foo\n", "Foo\r\n\n")]
	[DataRow("Foo\n", "Foo\n")]
	[DataRow("F\ro\n", "Foo\n\n")]
	[DataRow("FooBar \n", "FooBar \n\n")]
	[DataRow(" FooBar\n", " FooBar\n\n")]
	[DataRow("Foo\tBar\n", " Foo\tBar\n\n")]
	[DataRow("Foo\nFizz\nBuzz\n", "Foo\n\n\nFizz\n\n\nBuzz\n\n")]
	[DataRow("Foo\n\nFizz\n\nBuzz\n", "Foo\n\nFizz\n\nBuzz\n\n")]
	[DataRow("Foo\n\nFizz\nBuzz\n", "Foo\n\n\nFizz\n\n\nBuzz\n\n")]
	[DataRow("Foo\n\nFizz\n\nBuzz\n", "Foo\n\n\nFizz\n\nBuzz\n\n")]
	[DataRow("Foo\n\nFizz\n\nBuzz\n", "Foo\n\n\nFizz\n\n\nFuzz\n\n")]
	[DataRow("Foo\n\nFizz\n\nBuzz\n", "FooFizz\n\n\nBuzz\n\n")]
	[DataRow("Foo\n\nFizz\n\nBuzz\n", "Foo Fizz\n\n\nBuzz\n\n")]
	public void Compiler_BuildHtml_BadText(string rawText, string pulpText) {
		Assert.IsFalse(Compiler.TryBuildHtml(rawText, pulpText, out string _));
	}
}