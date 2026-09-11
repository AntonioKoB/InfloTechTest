using System.Threading;
using System.Threading.Tasks;

namespace UserManagement.Services.Commands;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Execute the command and return the id of the user it affected.
    /// </summary>
    Task<long> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
