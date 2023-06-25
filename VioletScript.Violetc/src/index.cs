using System;
using System.Collections.Generic;
using CommandLine;

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
                // resolve built-ins
                toDo();
                foreach (var src in options.Sources) {
                    toDo();
                }
            });
    }
}