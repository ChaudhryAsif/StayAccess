using Microsoft.EntityFrameworkCore;
using StayAccess.DAL.Extensions;
using StayAccess.DAL.Interfaces;
using StayAccess.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

namespace StayAccess.DAL.Repositories
{
    public class GenericService<T> : IGenericService<T> where T : class
    {
        private readonly StayAccessDbContext _dbContext;

        public GenericService(StayAccessDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Save()
        {
            try
            {
                _dbContext.SaveChanges();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Add(T entity)
        {
            try
            {
                _dbContext.Set<T>().Add(entity);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void AddWithSave(T entity)
        {
            try
            {
                Add(entity);
                Save();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Update(T entity)
        {
            try
            {
                _dbContext.Entry(entity).State = EntityState.Modified;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void UpdateWithSave(T entity)
        {
            try
            {
                Update(entity);
                Save();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void Delete(T entity)
        {
            try
            {
                _dbContext.Set<T>().Remove(entity);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DeleteWithSave(T entity)
        {
            try
            {
                Delete(entity);
                Save();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DeleteRange(IQueryable<T> entities)
        {
            try
            {
                _dbContext.Set<T>().RemoveRange(entities);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void DeleteRangeWithSave(IQueryable<T> entities)
        {
            try
            {
                DeleteRange(entities);
                Save();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public T Get(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return _dbContext.Set<T>().Where(predicate).FirstOrDefault();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<T> GetAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await _dbContext.Set<T>().Where(predicate).FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<T> GetAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {

            try
            {
                var query = _dbContext.Set<T>().AsQueryable<T>();

                if (includes != null)
                {
                    foreach (var include in includes)
                    {
                        query = query.Include(include);
                    }
                }

                return await query.Where(predicate).FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public T Get(Expression<Func<T, bool>> predicate, List<string> thenIncludes, params Expression<Func<T, object>>[] includes)
        {

            try
            {
                var query = _dbContext.Set<T>().AsQueryable<T>();

                if (includes != null)
                {
                    foreach (var include in includes)
                    {
                        query = query.Include(include);
                    }
                }

                if (thenIncludes != null)
                {
                    foreach (var thenInclude in thenIncludes)
                    {
                        query = query.Include(thenInclude);
                    }
                }

                return query.Where(predicate).FirstOrDefault();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public IQueryable<T> List()
        {
            try
            {
                return _dbContext.Set<T>().AsQueryable<T>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public IQueryable<T> List(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        {
            try
            {
                var query = _dbContext.Set<T>().AsQueryable<T>();

                if (includes != null)
                {
                    foreach (var include in includes)
                    {
                        query = query.Include(include);
                    }
                }

                // search
                if (predicate != null)
                    query = query.Where(predicate);

                return query;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public IQueryable<T> List(Expression<Func<T, bool>> predicate, List<string> thenIncludes, params Expression<Func<T, object>>[] includes)
        {
            try
            {
                var query = _dbContext.Set<T>().AsQueryable<T>();

                if (includes != null)
                {
                    foreach (var include in includes)
                    {
                        query = query.Include(include);
                    }
                }

                if (thenIncludes != null)
                {
                    foreach (var thenInclude in thenIncludes)
                    {
                        query = query.Include(thenInclude);
                    }
                }

                // search
                if (predicate != null)
                    query = query.Where(predicate);

                return query;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public IQueryable<T> List(params Expression<Func<T, object>>[] includes)
        {
            try
            {
                var query = _dbContext.Set<T>().AsQueryable<T>();

                if (includes != null)
                {
                    foreach (var include in includes)
                    {
                        query = query.Include(include);
                    }
                }

                return query;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public DataSourceResultDto PagedList(IQueryable<T> query, int pageNo, int pageSize, params Expression<Func<T, object>>[] includes)
        {
            try
            {
                int skipRecords = pageNo <= 1 ? 0 : (pageNo - 1) * pageSize;
                int totalRecords = query.Count();
                query = query.Skip(skipRecords).Take(pageSize);

                if (includes != null)
                {
                    foreach (var include in includes)
                    {
                        query = query.Include(include);
                    }
                }

                DataSourceResultDto result = new()
                {
                    Data = query.ToList(),
                    Total = totalRecords
                };

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public int Count(Expression<Func<T, bool>> predicate = null)
        {
            try
            {
                IQueryable<T> query = _dbContext.Set<T>();

                // search
                if (predicate != null)
                    query = query.Where(predicate);

                return query.Count();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public int Count(Expression<Func<T, bool>> predicate, List<string> thenIncludes, params Expression<Func<T, object>>[] includes)
        {
            try
            {
                var query = _dbContext.Set<T>().AsQueryable<T>();

                if (includes != null)
                {
                    foreach (var include in includes)
                    {
                        query = query.Include(include);
                    }
                }

                if (thenIncludes != null)
                {
                    foreach (var thenInclude in thenIncludes)
                    {
                        query = query.Include(thenInclude);
                    }
                }

                // search
                if (predicate != null)
                    query = query.Where(predicate);

                return query.Count();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public int Max(Expression<Func<T, int>> predicate)
        {
            try
            {
                return _dbContext.Set<T>().Max(predicate);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}