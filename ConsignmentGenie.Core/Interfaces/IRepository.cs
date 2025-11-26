using System.Linq.Expressions;

namespace ConsignmentGenie.Core.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<T?> GetAsync(Expression<Func<T, bool>> predicate, string includeProperties = "");
    Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, string includeProperties = "");
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}