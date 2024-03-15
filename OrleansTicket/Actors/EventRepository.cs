namespace OrleansTicket.Actors
{
    public interface IEventRepositoryGrain : IGrainWithGuidKey
    {
        Task AddEvent(Guid eventId);
        Task<Dictionary<Guid, IEventGrain>> GetEvents();
    }
    // TODO: Check in cluster deployment
    /// <summary>
    /// EventRepository grain responsible for aggregating all events data in a system.
    /// </summary>
    public sealed class EventRepository : Grain, IEventRepositoryGrain
    {
        private Dictionary<Guid , IEventGrain> _events = new();

        public Task AddEvent(Guid eventId)
        {
            _events[eventId] = GrainFactory.GetGrain<IEventGrain>(eventId);
            return Task.CompletedTask;
        }
        public Task<Dictionary<Guid, IEventGrain>> GetEvents()
        {
            return Task.FromResult(_events);
        }
    }
}
