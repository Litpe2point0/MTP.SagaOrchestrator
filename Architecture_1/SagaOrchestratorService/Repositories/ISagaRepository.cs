using SagaOrchestratorService.Models;

namespace SagaOrchestratorService.Repositories
{
    public interface ISagaRepository
    {
        Task<SagaInstance?> GetSagaInstanceAsync(Guid sagaId);
        Task<IEnumerable<SagaInstance>> GetActiveSagasAsync();
        Task<IEnumerable<SagaInstance>> GetSagasByStatusAsync(SagaStatus status);
        Task<Guid> CreateSagaInstanceAsync(SagaInstance sagaInstance);
        Task UpdateSagaInstanceAsync(SagaInstance sagaInstance);
        Task DeleteSagaInstanceAsync(Guid sagaId);
        Task<bool> SagaExistsAsync(Guid sagaId);
    }
}
