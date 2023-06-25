using System;
using System.Collections.Generic;

class VioletcCli {
    public class CliOptions {
        [CommandLine.Value(0, MetaName = "sources", Required = true, HelpText = "Sources to compile.")]
        public List<String> Sources { get; set; }
    }

    static void Main(string[] args) {
        CommandLine.Parser.Default.ParseArguments<CliOptions>(args)
            .WithParsed<CliOptions>(options => {
                foreach (var src in o.Sources) {
                    Console.WriteLine(src);
                }
            });
    }
}