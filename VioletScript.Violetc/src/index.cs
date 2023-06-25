using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using Vsp = VioletScript.Parser;
using VspScript = VioletScript.Parser.Source.Script;
using VspVerifier = VioletScript.Parser.Verifier.Verifier;

class VioletcCli {
    public class CliOptions {
        [CommandLine.Value(0, MetaName = "sources", Required = true, HelpText = "Sources to compile.")]
        public IEnumerable<string> Sources { get; set; }
        [CommandLine.Option('o', "output", Default = "", Required = false, HelpText = "Output path.")]
        public string Output { get; set; }
    }

    static void Main(string[] args) {
        CommandLine.Parser.Default.ParseArguments<CliOptions>(args)
            .WithParsed<CliOptions>(options => {
                typeCheck(options);
            });
    }

    private static VspVerifier verifier = new VspVerifier();

    private static void typeCheck(CliOptions options) {
        // resolve built-ins
        verifier.Options.AllowDuplicates = true;
        if (!typeCheckSingleSource(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "builtins"))) {
            return;
        }
        verifier.Options.AllowDuplicates = false;

        foreach (var src in options.Sources) {
            if (!typeCheckSingleSource(src)) {
                break;
            }
        }

        if (verifier.AllProgramsAreValid) {
            // valid
        }
    }

    private static bool typeCheckSingleSource(String fileName) {
        fileName = Path.GetFullPath(fileName);
        // directory/index.vs
        try {
            fileName = File.GetAttributes(fileName).HasFlag(FileAttributes.Directory) ? Path.Combine(fileName, "./index.vs") : fileName;
        } catch {
        }
        fileName = Path.ChangeExtension(fileName, ".vs");
        string source = "";
        try {
            source = File.ReadAllText(fileName);
        } catch (IOException) {
            Console.WriteLine("Failed to read file: " + fileName + ".");
            return false;
        }
        var script = new VspScript(fileName, source);
        var parser = new Vsp.Parser.Parser(script);
        var program = parser.ParseProgram();
        if (program != null) {
            verifier.VerifyPrograms(new List<Vsp.Ast.Program>{program});
        }

        script.SortDiagnostics();
        script.SortDiagnosticsForIncludedScripts();

        foreach (var d in script.DiagnosticsFromCurrentAndIncludedScripts) {
            Console.WriteLine(d.ToString());
        }
        return script.IsValid && verifier.AllProgramsAreValid;
    }
}