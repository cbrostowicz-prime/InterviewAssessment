using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace OrderHistory.Tests.Infrastructure;

public class CapturingInterceptor : DbCommandInterceptor
{
    public List<string> ExecutedCommands { get; } = [];

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ExecutedCommands.Add(command.CommandText);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}
