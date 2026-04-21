using ThorFlasher.Core.Models;

namespace ThorFlasher.Core.Interfaces;

public interface IThorScriptResolver
{
    ScriptCommandDefinition GetSendBuildCommand(OperationContext context);

    ScriptCommandDefinition GetOperationCommand(OperationContext context);
}
