using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UserManagement.Services.Commands;
using UserManagement.Services.Messaging;

namespace UserManagement.Data.Tests;

/// <summary>
/// Reads what a test published onto a bus without a worker running: takes up to <paramref name="count"/>
/// commands and gives up after the timeout, so "exactly one was published" is a read of two that yields one.
/// </summary>
internal static class MessageBusTestExtensions
{
    public static async Task<List<ICommand>> ReadAsync(this IMessageBus bus, int count, TimeSpan? timeout = null)
    {
        using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(5));
        var read = new List<ICommand>();
        try
        {
            await foreach (var command in bus.ConsumeAsync(cts.Token))
            {
                read.Add(command);
                if (read.Count == count)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            // The timeout elapsed with fewer commands than asked for; the caller asserts on the count.
        }

        return read;
    }
}
