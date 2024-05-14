using CommandLine;

namespace BPXProjector;

public class ProgramArguments {
    public enum Command { @new }
    [Value(0, Required = true, Default =Command.@new, HelpText = "Which command to run.")]
    public Command? SubCommand { get; set; }

    public int Success()
    {
        Console.WriteLine(SubCommand);
        return 0;
    }
}