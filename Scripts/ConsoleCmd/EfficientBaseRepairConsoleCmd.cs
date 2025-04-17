using System.Collections.Generic;

public class EfficientBaseRepairConsoleCmd : ConsoleCmdAbstract
{
    private static readonly Logging.Logger logger = Logging.CreateLogger("EfficientBaseRepairConsoleCmd");

    public override string[] getCommands()
    {
        return new string[] { "efficientbaserepair", "ebr" };
    }

    public override string getDescription()
    {
        return "efficientbaserepair ebr => command line tools for the mod EfficientBaseRepair.";
    }

    public override string getHelp()
    {
        return @"EfficientBaseRepair commands:
            - blablabla
        ";
    }
    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        var args = _params.ToArray();

        if (args.Length == 0)
        {
            Log.Out(getHelp());
            return;
        }

        switch (args[0].ToLower())
        {
            default:
                logger.Error($"Invalid or not implemented command: '{_params[0]}'");
                break;
        }
    }
}