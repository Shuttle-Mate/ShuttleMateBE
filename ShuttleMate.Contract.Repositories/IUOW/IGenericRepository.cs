using System.Linq.Expressions;
using ShuttleMate.Core.Bases;

namespace ShuttleMate.Contract.Repositories.IUOW
{
    public interface IGenericRepository<T> where T : class
    {
        // query
        IQueryable<T> Entities { get; }

        // non async
        IEnumerable<T> GetAll();
        T? GetById(object id);
        void Insert(T obj);
        void InsertRange(IList<T> obj);
        void Update(T obj);
        void Delete(T entity);
        void Save();

        // async
        Task<IList<T>> GetAllAsync();
        Task<BasePaginatedList<T>> GetPagging(IQueryable<T> query, int index, int pageSize);
        Task<T?> GetByIdAsync(object id);
        Task InsertAsync(T obj);
        Task UpdateAsync(T obj);
        Task DeleteAsync(object id);
        Task DeleteAsync(T entity);
        Task SaveAsync();
        Task<T> FindAsync(Expression<Func<T, bool>> predicate);
        Task<List<T>> FindAllAsync(Expression<Func<T, bool>> predicate);
        Task<IQueryable<T>> GetAllQueryableAsync();
        IQueryable<T> GetQueryable();
    }
}
