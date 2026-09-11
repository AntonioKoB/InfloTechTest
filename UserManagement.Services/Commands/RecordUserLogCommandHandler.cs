using System;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Data;

namespace UserManagement.Services.Commands;

public class RecordUserLogCommandHandler : ICommandHandler<RecordUserLogCommand>
{
    public RecordUserLogCommandHandler(IDataContext dataAccess)
    {
    }

    public Task<long> HandleAsync(RecordUserLogCommand command, CancellationToken cancellationToken) => throw new NotImplementedException();
}
