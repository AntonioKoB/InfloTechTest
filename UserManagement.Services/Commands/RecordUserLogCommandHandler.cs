using System.Threading;
using System.Threading.Tasks;
using UserManagement.Data;

namespace UserManagement.Services.Commands;

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
