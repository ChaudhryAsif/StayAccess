using StayAccess.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace StayAccess.DAL.Interfaces
{
    public interface IGenericService<T> where T : class
    {
        void Save();

        void Add(T entity);

        void AddWithSave(T entity);

        void Update(T entity);

        void UpdateWithSave(T entity);

        void Delete(T entity);

        void DeleteWithSave(T entity);

        void DeleteRange(IQueryable<T> entities);

        void DeleteRangeWithSave(IQueryable<T> entities);

        T Get(Expression<Func<T, bool>> predicate);

        Task<T> GetAsync(Expression<Func<T, bool>> predicate);

        Task<T> GetAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        T Get(Expression<Func<T, bool>> predicate, List<string> thenIncludes, params Expression<Func<T, object>>[] includes);

        IQueryable<T> List(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

        IQueryable<T> List(Expression<Func<T, bool>> predicate, List<string> thenIncludes, params Expression<Func<T, object>>[] includes);

        IQueryable<T> List(params Expression<Func<T, object>>[] includes);

        DataSourceResultDto PagedList(IQueryable<T> query, int pageNo, int pageSize, params Expression<Func<T, object>>[] includes);

        int Count(Expression<Func<T, bool>> predicate = null);

        int Count(Expression<Func<T, bool>> predicate, List<string> thenIncludes, params Expression<Func<T, object>>[] includes);

        int Max(Expression<Func<T, int>> predicate);
    }
}
