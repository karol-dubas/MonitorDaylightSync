using MonitorDaylightSync.Dtos;

namespace MonitorDaylightSync.CommandExecutors;

public interface ICommandExecutor
{
    Task ExecuteAsync(MonitorCommandDto dto, CancellationToken ct);
}