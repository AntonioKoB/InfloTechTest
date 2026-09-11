using System.Threading;
using System.Threading.Tasks;
using UserManagement.Data;

namespace UserManagement.Services.Commands;

/// <summary>
/// Writes an audit entry that was built where the action happened. Nothing is recomputed here, so the entry
/// reflects the state at the time of the action however long it waited on the bus.
/// </summary>
public class RecordUserLogCommandHandler : ICommandHandler<RecordUserLogCommand>
{
    private readonly IDataContext _dataAccess;

    public RecordUserLogCommandHandler(IDataContext dataAccess) => _dataAccess = dataAccess;

    public async Task<long> HandleAsync(RecordUserLogCommand command, CancellationToken cancellationToken)
    {
        await _dataAccess.CreateAsync(command.Entry);
        return command.Entry.UserId;
    }
}
