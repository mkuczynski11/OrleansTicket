using Microsoft.Extensions.Logging;

namespace OrleansTicket.Actors
{
    public interface IEventRepositoryGrain : IGrainWithGuidKey
    {
        Task AddEvent(Guid eventId);
        Task<Dictionary<Guid, IEventGrain>> GetEvents();
    }
    /// <summary>
    /// EventRepository grain responsible for aggregating all events data in a system.
    /// </summary>
    public sealed class EventRepository : Grain, IEventRepositoryGrain
    {
        ILogger<EventRepository> _logger;
        public EventRepository(ILogger<EventRepository> logger)
        {
            _logger = logger;
        }
        private Dictionary<Guid , IEventGrain> _events = new();
        public Task AddEvent(Guid eventId)
        {
            _logger.LogInformation($"Adding event {eventId}");
            _events[eventId] = GrainFactory.GetGrain<IEventGrain>(eventId);
            return Task.CompletedTask;
        }
        public Task<Dictionary<Guid, IEventGrain>> GetEvents()
        {
            _logger.LogInformation($"Getting all events");
            return Task.FromResult(_events);
        }
    }
}
