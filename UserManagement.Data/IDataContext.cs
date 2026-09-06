using System.Collections.Generic;
using System.Threading.Tasks;

namespace UserManagement.Data;

public interface IDataContext
{
    /// <summary>
    /// Get a list of items
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <returns></returns>
    Task<IEnumerable<TEntity>> GetAllAsync<TEntity>() where TEntity : class;

    /// <summary>
    /// Get a single item matching the given ID, or null if none exists
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<TEntity?> GetByIdAsync<TEntity>(object id) where TEntity : class;

    /// <summary>
    /// Create a new item
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task CreateAsync<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Update an existing item matching the ID
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task UpdateAsync<TEntity>(TEntity entity) where TEntity : class;

    /// <summary>
    /// Delete an existing item matching the ID
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task DeleteAsync<TEntity>(TEntity entity) where TEntity : class;
}
