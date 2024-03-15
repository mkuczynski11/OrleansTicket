namespace OrleansTicket.Actors
{
    public interface IEventQueryGrain : IGrainWithGuidKey
    {
        Task<List<MinimalEventData>> GetAllEvents(string name);
    }
    public class EventQuery : Grain, IEventQueryGrain
    {
        // TODO: Add timeout
        // TODO: Add retry mechanism
        public async Task<List<MinimalEventData>> GetAllEvents(string name)
        {
            name = name == null ? string.Empty : name;
            var eventRepository = GrainFactory.GetGrain<IEventRepositoryGrain>(Guid.Empty);
            var events = await eventRepository.GetEvents();
            List<Task<MinimalEventData>> tasks = new List<Task<MinimalEventData>>();
            foreach (var item in events)
            {
                tasks.Add(item.Value.GetMinimalInfo());
            }

            var eventInfos = await Task.WhenAll(tasks);

            return eventInfos.Where(x => x.Name != null && x.Name.ToLower().StartsWith(name.ToLower())).ToList();
        }
    }
}
