using System.Reflection;

namespace OrleansTicket.Actors
{
    public interface IEventQueryGrain : IGrainWithGuidKey
    {
        Task<List<MinimalEventData>> GetAllEvents(string name);
    }
    /// <summary>
    /// EventQuery grain responsible for aggregating all events data into one response.
    /// </summary>
    public class EventQuery : Grain, IEventQueryGrain
    {
        // TODO: Add retry mechanism
        public async Task<List<MinimalEventData>> GetAllEvents(string name)
        {
            name = name == null ? string.Empty : name;
            var eventRepository = GrainFactory.GetGrain<IEventRepositoryGrain>(Guid.Empty);
            var events = await eventRepository.GetEvents();

            var cancellationTask = Task.Delay(1000);

            var tasks = new List<Task>(events.Select(item => item.Value.GetMinimalInfo()).ToList());
            tasks.Add(cancellationTask);

            List<MinimalEventData> result = new();

            while(tasks.Count > 1)
            {
                var completed = await Task.WhenAny(tasks);
                if (completed is Task<MinimalEventData> completedEventTask)
                {
                    result.Add(completedEventTask.Result);
                }
                else
                {
                    // Could cancel remaining tasks if they would support CancellationToken, but Task.FromResult does not
                    break;
                }
                tasks.Remove(completed);
            }

            return result.Where(x => x.Name != null && x.Name.ToLower().StartsWith(name.ToLower())).ToList();
        }
    }
}
