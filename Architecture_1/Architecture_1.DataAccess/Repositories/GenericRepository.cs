using Architecture_1.DataAccess.Data;
using Architecture_1.DataAccess.Repositories.interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Architecture_1.DataAccess.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _appDbContext;
        internal DbSet<T> _dbSet;

        public GenericRepository(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
            _dbSet = _appDbContext.Set<T>();
        }

        public IQueryable<T> FindAll(Expression<Func<T, bool>>? predicate = null, params Expression<Func<T, object>>[] includeProperties)
        {
            IQueryable<T> items = _appDbContext.Set<T>();

            if (includeProperties.Any())
            {
                foreach (var property in includeProperties)
                {
                    items = items.Include(property);
                }
            }


            if (predicate != null)
            {
                items = items.Where(predicate);
            }

            return items;
        }

        public async Task<T?> FindByIdAsync(object id, params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _appDbContext.Set<T>().AsQueryable();

            // Apply includes
            foreach (var includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            // Get Id property info
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Entity {typeof(T).Name} does not have an 'Id' property");

            var idType = Nullable.GetUnderlyingType(idProperty.PropertyType) ?? idProperty.PropertyType;

            // Handle specific problematic types that can't be compared implicitly
            if (idType == typeof(Guid))
            {
                var guidId = id is Guid g ? g :
                            Guid.TryParse(id?.ToString(), out var parsed) ? parsed :
                            throw new ArgumentException($"Cannot convert '{id}' to Guid", nameof(id));
                return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == guidId);
            }

            if (idType == typeof(DateTime))
            {
                var dateId = id is DateTime dt ? dt :
                            DateTime.TryParse(id?.ToString(), out var parsed) ? parsed :
                            throw new ArgumentException($"Cannot convert '{id}' to DateTime", nameof(id));
                return await query.FirstOrDefaultAsync(e => EF.Property<DateTime>(e, "Id") == dateId);
            }

            if (idType == typeof(decimal))
            {
                var decId = id is decimal dec ? dec :
                           decimal.TryParse(id?.ToString(), out var parsed) ? parsed :
                           throw new ArgumentException($"Cannot convert '{id}' to decimal", nameof(id));
                return await query.FirstOrDefaultAsync(e => EF.Property<decimal>(e, "Id") == decId);
            }

            // For safe types (int, long, string, etc.) - use generic approach
            try
            {
                var convertedId = Convert.ChangeType(id, idType);

                // Create expression: e => e.Id == convertedId
                var parameter = Expression.Parameter(typeof(T), "e");
                var property = Expression.Property(parameter, "Id");
                var constant = Expression.Constant(convertedId, idProperty.PropertyType);
                var equality = Expression.Equal(property, constant);
                var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

                return await query.FirstOrDefaultAsync(lambda);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Cannot convert id '{id}' of type {id?.GetType().Name} to {idType.Name}: {ex.Message}", nameof(id));
            }
        }

        public async Task<T?> FindByIdWithPaths(object id, params string[] includePaths)
        {
            IQueryable<T> query = _dbSet;

            // Apply string-based includes
            foreach (var include in includePaths)
            {
                query = query.Include(include);
            }

            // Get Id property info
            var idProperty = typeof(T).GetProperty("Id");
            if (idProperty == null)
                throw new InvalidOperationException($"Entity {typeof(T).Name} does not have an 'Id' property");

            var idType = Nullable.GetUnderlyingType(idProperty.PropertyType) ?? idProperty.PropertyType;

            // Handle specific problematic types that can't be compared implicitly
            if (idType == typeof(Guid))
            {
                var guidId = id is Guid g ? g :
                            Guid.TryParse(id?.ToString(), out var parsed) ? parsed :
                            throw new ArgumentException($"Cannot convert '{id}' to Guid", nameof(id));
                return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == guidId);
            }

            if (idType == typeof(DateTime))
            {
                var dateId = id is DateTime dt ? dt :
                            DateTime.TryParse(id?.ToString(), out var parsed) ? parsed :
                            throw new ArgumentException($"Cannot convert '{id}' to DateTime", nameof(id));
                return await query.FirstOrDefaultAsync(e => EF.Property<DateTime>(e, "Id") == dateId);
            }

            if (idType == typeof(decimal))
            {
                var decId = id is decimal dec ? dec :
                           decimal.TryParse(id?.ToString(), out var parsed) ? parsed :
                           throw new ArgumentException($"Cannot convert '{id}' to decimal", nameof(id));
                return await query.FirstOrDefaultAsync(e => EF.Property<decimal>(e, "Id") == decId);
            }

            // For safe types (int, long, string, etc.) - use generic approach
            try
            {
                var convertedId = Convert.ChangeType(id, idType);

                // Create expression: e => e.Id == convertedId
                var parameter = Expression.Parameter(typeof(T), "e");
                var property = Expression.Property(parameter, "Id");
                var constant = Expression.Constant(convertedId, idProperty.PropertyType);
                var equality = Expression.Equal(property, constant);
                var lambda = Expression.Lambda<Func<T, bool>>(equality, parameter);

                return await query.FirstOrDefaultAsync(lambda);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Cannot convert id '{id}' of type {id?.GetType().Name} to {idType.Name}: {ex.Message}", nameof(id));
            }
        }

        public IQueryable<T> FindAllWithPaths(Expression<Func<T, bool>>? predicate = null, params string[] includePaths)
        {
            IQueryable<T> query = _dbSet;

            // Apply string-based includes
            foreach (var include in includePaths)
            {
                query = query.Include(include);
            }

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return query;
        }

        public async Task<T?> CreateAsync(T entity)
        {
            var entry = await _dbSet.AddAsync(entity);
            await _appDbContext.SaveChangesAsync();
            return entry.Entity;
        }

        public async Task<T?> UpdateAsync(object id, T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity), "Entity không được để null.");
            }

            var existingEntity = await _dbSet.FindAsync(id);
            if (existingEntity == null)
            {
                Console.WriteLine($"Không tìm thấy entity với ID {id}.");
                return null;
            }

            _appDbContext.Entry(existingEntity).State = EntityState.Detached;
            _appDbContext.Entry(entity).State = EntityState.Modified;


            await _appDbContext.SaveChangesAsync();
            return entity;
        }

        public async Task<T?> DeleteAsync(object id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                await _appDbContext.SaveChangesAsync();
                return entity;
            }

            return null;
        }



    }


}
