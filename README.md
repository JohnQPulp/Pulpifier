## The Pulpifier

The repository contains the C# library and CLI tool for "compiling" metadata-enhanced public domain text files into visual novel html content.

### Library

The ASP.NET website https://github.com/JohnQPulp/PublicDomainPulp includes the Pulpifier as a submodule, calling it during startup to build local visual novels that can then be accessed via localhost. You can also just access the pre-built visual novels online at https://publicdomainpulp.com/. See the website and source repo for more details and for example library-type usage.

### CLI

The Pulpifier also works as a standalone CLI tool. To start, ensure you have .NET 10 installed: https://dotnet.microsoft.com/download

Check out and build the solution like:

```bash
git clone https://github.com/JohnQPulp/Pulpifier.git
cd Pulpifier
dotnet build Pulpifier.slnx
```

You can then build a visual novel by pointing the CLI towards a VN directory with the following elements:

* A book.txt file (the original text)
* A pulp.txt file (metadata-enhanced text)
* A metadata.json file
* An images/ subdirectory

You can try it out by using one of the sample directories under examples/:

```bash
./PulpifierCLI/bin/Debug/net10.0/PulpifierCLI examples/wizard/
```

This will produce an examples/wizard/out.html file. The static out.html file plus the images/ files form the entire visual novel, which you can then just open in the browser.

You can also test using a full visual novel repo e.g.:

```bash
git clone https://github.com/JohnQPulp/CupOfGold.git
./PulpifierCLI/bin/Debug/net10.0/PulpifierCLI CupOfGold/
```

These static file-based visual novels don't contain all the features as the web pages produced by the https://github.com/JohnQPulp/PublicDomainPulp website, but are good for prototyping.
