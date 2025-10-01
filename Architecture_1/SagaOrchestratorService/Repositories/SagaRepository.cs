using Microsoft.EntityFrameworkCore;
using SagaOrchestratorService.Models;
using Microsoft.Extensions.Logging;

namespace SagaOrchestratorService.Repositories
{
    public class SagaRepository : ISagaRepository
    {
        private readonly SagaDBContext _context;
        private readonly ILogger<SagaRepository> _logger;

        public SagaRepository(SagaDBContext context, ILogger<SagaRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<SagaInstance?> GetSagaInstanceAsync(Guid sagaId)
        {
            try
            {
                var entity = await _context.SagaInstances
                    .Include(s => s.Steps)
                    .FirstOrDefaultAsync(s => s.SagaId == sagaId);

                return entity?.ToSagaInstance();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving saga instance {SagaId}", sagaId);
                throw;
            }
        }

        public async Task<IEnumerable<SagaInstance>> GetActiveSagasAsync()
        {
            try
            {
                var entities = await _context.SagaInstances
                    .Include(s => s.Steps)
                    .Where(s => s.Status == SagaStatus.Running || s.Status == SagaStatus.RollingBack)
                    .ToListAsync();

                return entities.Select(e => e.ToSagaInstance());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving active sagas");
                throw;
            }
        }

        public async Task<IEnumerable<SagaInstance>> GetSagasByStatusAsync(SagaStatus status)
        {
            try
            {
                var entities = await _context.SagaInstances
                    .Include(s => s.Steps)
                    .Where(s => s.Status == status)
                    .ToListAsync();

                return entities.Select(e => e.ToSagaInstance());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sagas by status {Status}", status);
                throw;
            }
        }

        public async Task<Guid> CreateSagaInstanceAsync(SagaInstance sagaInstance)
        {
            try
            {
                var entity = SagaInstanceEntity.FromSagaInstance(sagaInstance);
                
                _context.SagaInstances.Add(entity);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Created saga instance {SagaId}", sagaInstance.SagaId);
                return entity.SagaId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating saga instance {SagaId}", sagaInstance.SagaId);
                throw;
            }
        }

        public async Task UpdateSagaInstanceAsync(SagaInstance sagaInstance)
        {
            const int maxRetries = 3;
            var retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    // Detach any existing tracked entities to avoid conflicts
                    DetachExistingEntity(sagaInstance.SagaId);

                    var existingEntity = await _context.SagaInstances
                        .Include(s => s.Steps)
                        .FirstOrDefaultAsync(s => s.SagaId == sagaInstance.SagaId);

                    if (existingEntity == null)
                    {
                        throw new InvalidOperationException($"Saga instance {sagaInstance.SagaId} not found");
                    }

                    // Update main properties
                    existingEntity.UpdateFromSagaInstance(sagaInstance);

                    // Update steps - remove old ones and add current ones
                    _context.SagaStepExecutions.RemoveRange(existingEntity.Steps);
                    
                    existingEntity.Steps = sagaInstance.Steps
                        .Select(s => SagaStepExecutionEntity.FromSagaStepExecution(s, sagaInstance.SagaId))
                        .ToList();

                    await _context.SaveChangesAsync();
                    
                    _logger.LogDebug("Updated saga instance {SagaId}", sagaInstance.SagaId);
                    return; // Success
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    retryCount++;
                    _logger.LogWarning("Concurrency conflict updating saga {SagaId}, retry {RetryCount}/{MaxRetries}", 
                        sagaInstance.SagaId, retryCount, maxRetries);

                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Failed to update saga {SagaId} after {MaxRetries} retries due to concurrency conflicts", 
                            sagaInstance.SagaId, maxRetries);
                        throw;
                    }

                    // Clear the context to reload fresh data
                    _context.ChangeTracker.Clear();
                    
                    // Exponential backoff
                    var delay = TimeSpan.FromMilliseconds(50 * Math.Pow(2, retryCount - 1));
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating saga instance {SagaId}", sagaInstance.SagaId);
                    throw;
                }
            }
        }

        private void DetachExistingEntity(Guid sagaId)
        {
            var existingEntry = _context.ChangeTracker.Entries<SagaInstanceEntity>()
                .FirstOrDefault(e => e.Entity.SagaId == sagaId);
                
            if (existingEntry != null)
            {
                existingEntry.State = EntityState.Detached;
            }

            // Also detach related step entities
            var stepEntries = _context.ChangeTracker.Entries<SagaStepExecutionEntity>()
                .Where(e => e.Entity.SagaId == sagaId)
                .ToList();
                
            foreach (var entry in stepEntries)
            {
                entry.State = EntityState.Detached;
            }
        }

        public async Task DeleteSagaInstanceAsync(Guid sagaId)
        {
            try
            {
                var entity = await _context.SagaInstances.FindAsync(sagaId);
                if (entity != null)
                {
                    _context.SagaInstances.Remove(entity);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Deleted saga instance {SagaId}", sagaId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting saga instance {SagaId}", sagaId);
                throw;
            }
        }

        public async Task<bool> SagaExistsAsync(Guid sagaId)
        {
            try
            {
                return await _context.SagaInstances.AnyAsync(s => s.SagaId == sagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if saga exists {SagaId}", sagaId);
                throw;
            }
        }
    }
}
