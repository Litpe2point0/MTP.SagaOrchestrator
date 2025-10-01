using SagaOrchestratorService.Models;
using SagaOrchestratorService.Repositories;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace SagaOrchestratorService.Services
{
    public interface ISagaUpdateQueue
    {
        Task QueueUpdateAsync(Guid sagaId, Func<SagaInstance, Task> updateAction);
        Task QueueUpdateAsync(Guid sagaId, Func<SagaInstance, Task<SagaInstance>> updateFunction);
    }

    public class SagaUpdateQueue : ISagaUpdateQueue, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SagaUpdateQueue> _logger;
        private readonly ConcurrentDictionary<Guid, Channel<UpdateRequest>> _sagaQueues;
        private readonly ConcurrentDictionary<Guid, Task> _processingTasks;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public SagaUpdateQueue(IServiceProvider serviceProvider, ILogger<SagaUpdateQueue> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _sagaQueues = new ConcurrentDictionary<Guid, Channel<UpdateRequest>>();
            _processingTasks = new ConcurrentDictionary<Guid, Task>();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task QueueUpdateAsync(Guid sagaId, Func<SagaInstance, Task> updateAction)
        {
            await QueueUpdateAsync(sagaId, async saga =>
            {
                await updateAction(saga);
                return saga;
            });
        }

        public async Task QueueUpdateAsync(Guid sagaId, Func<SagaInstance, Task<SagaInstance>> updateFunction)
        {
            var updateRequest = new UpdateRequest
            {
                UpdateFunction = updateFunction,
                CompletionSource = new TaskCompletionSource<bool>()
            };

            var channel = GetOrCreateChannel(sagaId);
            
            try
            {
                await channel.Writer.WriteAsync(updateRequest, _cancellationTokenSource.Token);
                await updateRequest.CompletionSource.Task; // Wait for completion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue update for saga {SagaId}", sagaId);
                throw;
            }
        }

        private Channel<UpdateRequest> GetOrCreateChannel(Guid sagaId)
        {
            return _sagaQueues.GetOrAdd(sagaId, _ =>
            {
                var channel = Channel.CreateUnbounded<UpdateRequest>();
                
                // Start processing task for this saga
                var processingTask = ProcessSagaUpdatesAsync(sagaId, channel.Reader);
                _processingTasks.TryAdd(sagaId, processingTask);
                
                return channel;
            });
        }

        private async Task ProcessSagaUpdatesAsync(Guid sagaId, ChannelReader<UpdateRequest> reader)
        {
            _logger.LogDebug("Started processing queue for saga {SagaId}", sagaId);
            
            try
            {
                await foreach (var request in reader.ReadAllAsync(_cancellationTokenSource.Token))
                {
                    await ProcessSingleUpdateAsync(sagaId, request);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Processing queue for saga {SagaId} was cancelled", sagaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queue for saga {SagaId}", sagaId);
            }
            finally
            {
                // Cleanup when done
                _sagaQueues.TryRemove(sagaId, out _);
                _processingTasks.TryRemove(sagaId, out _);
                _logger.LogDebug("Stopped processing queue for saga {SagaId}", sagaId);
            }
        }

        private async Task ProcessSingleUpdateAsync(Guid sagaId, UpdateRequest request)
        {
            const int maxRetries = 3;
            var retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var sagaRepository = scope.ServiceProvider.GetRequiredService<ISagaRepository>();

                    // Load the latest saga instance
                    var sagaInstance = await sagaRepository.GetSagaInstanceAsync(sagaId);
                    
                    if (sagaInstance == null)
                    {
                        _logger.LogWarning("Saga {SagaId} not found during update processing", sagaId);
                        request.CompletionSource.SetException(new InvalidOperationException($"Saga {sagaId} not found"));
                        return;
                    }

                    // Apply the update function
                    var updatedSaga = await request.UpdateFunction(sagaInstance);

                    // Save the updated saga
                    await sagaRepository.UpdateSagaInstanceAsync(updatedSaga);
                    
                    _logger.LogDebug("Successfully processed update for saga {SagaId}", sagaId);
                    request.CompletionSource.SetResult(true);
                    return;
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
                {
                    retryCount++;
                    _logger.LogWarning("Concurrency conflict for saga {SagaId}, retry {RetryCount}/{MaxRetries}", 
                        sagaId, retryCount, maxRetries);

                    if (retryCount >= maxRetries)
                    {
                        _logger.LogError(ex, "Failed to update saga {SagaId} after {MaxRetries} retries due to concurrency conflicts", 
                            sagaId, maxRetries);
                        request.CompletionSource.SetException(ex);
                        return;
                    }

                    // Exponential backoff
                    var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryCount - 1));
                    await Task.Delay(delay, _cancellationTokenSource.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing update for saga {SagaId}", sagaId);
                    request.CompletionSource.SetException(ex);
                    return;
                }
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            
            // Complete all channels
            foreach (var (sagaId, channel) in _sagaQueues)
            {
                channel.Writer.Complete();
            }

            // Wait for all processing tasks to complete (with timeout)
            var allTasks = _processingTasks.Values.ToArray();
            if (allTasks.Length > 0)
            {
                try
                {
                    Task.WaitAll(allTasks, TimeSpan.FromSeconds(30));
                }
                catch (AggregateException)
                {
                    // Ignore exceptions during shutdown
                }
            }

            _cancellationTokenSource.Dispose();
        }

        private class UpdateRequest
        {
            public required Func<SagaInstance, Task<SagaInstance>> UpdateFunction { get; set; }
            public required TaskCompletionSource<bool> CompletionSource { get; set; }
        }
    }
}