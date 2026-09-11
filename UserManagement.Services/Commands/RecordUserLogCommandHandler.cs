using System.Threading;
using System.Threading.Tasks;
using UserManagement.Data;

namespace UserManagement.Services.Commands;

/// <summary>
/// Writes the entry as built; nothing is recomputed.
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
