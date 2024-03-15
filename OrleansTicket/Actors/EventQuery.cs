using OrleansTicket.Exception;
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
        private static double ExternalCallSimulationFailProbability = 0.5f;
        private static int MaxRetries = 5;
        public async Task<List<MinimalEventData>> GetAllEvents(string name)
        {
            int retries = 0;
            while (retries < MaxRetries)
            {
                try
                {
                    return await this.GetEvents(name);
                }
                catch (NoConnectionException)
                {
                    Console.WriteLine("Error fetching events. Retrying");
                    retries++;
                }
            }

            throw new System.Exception("Failed to fetch events!!!");

        }
        private async Task<List<MinimalEventData>> GetEvents(string name)
        {
            Console.WriteLine("Starting to query events!");
            Random rnd = new Random();
            if (rnd.NextDouble() < ExternalCallSimulationFailProbability)
            {
                Console.WriteLine("EventQuery failed because of external call error");
                throw new NoConnectionException();
            }

            name = name == null ? string.Empty : name;
            var eventRepository = GrainFactory.GetGrain<IEventRepositoryGrain>(Guid.Empty);
            var events = await eventRepository.GetEvents();

            var cancellationTask = Task.Delay(1000);

            var tasks = new List<Task>(events.Select(item => item.Value.GetMinimalInfo()).ToList());
            tasks.Add(cancellationTask);

            List<MinimalEventData> result = new();

            while (tasks.Count > 1)
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
