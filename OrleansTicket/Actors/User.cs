using Orleans;
using Orleans.Runtime;
using OrleansTicket.Controllers;
using OrleansTicket.Exception;
using System.Xml.Linq;

namespace OrleansTicket.Actors
{
    public interface IUserGrain : IGrainWithStringKey
    {
        Task<string> InitializeUser(string name, string surname);
        Task UpdateUserInfo(string name, string surname);
        Task AddReservation(string reservationId);
        Task<FullUserDetails> GetUserInfo();
        Task<bool> IsInitialized();
    }
    /// <summary>
    /// Grain representing User entity with data describing a particular user.
    /// User's state is persisted apart from the reservation list(which is the case because of Reservation not being persisted).
    /// </summary>
    /// <param name="state">User's persistent state</param>
    public sealed class UserGrain(
        [PersistentState(stateName: "user", storageName: "users")] IPersistentState<UserDetails> state, ILogger<UserGrain> logger): Grain, IUserGrain
    {

        private List<string> Reservations { get; set; } = new();
        public async Task<string> InitializeUser(string name, string surname)
        {
            logger.LogInformation($"Initializing user for {this.GetPrimaryKeyString()}");
            if (state.State.IsInitialized)
            {
                throw new UserExistsException();
            }

            state.State = new()
            {
                Name = name,
                Surname = surname,
                IsInitialized = true
            };

            await state.WriteStateAsync();
            return this.GetPrimaryKeyString();
        }

        public async Task UpdateUserInfo(string name, string surname)
        {
            logger.LogInformation($"Updating user info for {this.GetPrimaryKeyString()}");
            if (!state.State.IsInitialized)
            {
                throw new UserDoesNotExistException();
            }
            state.State.Name = name;
            state.State.Surname = surname;

            await state.WriteStateAsync();
        }

        public async Task AddReservation(string reservationId)
        {
            logger.LogInformation($"Adding reservation {reservationId} for user {this.GetPrimaryKeyString()}");
            if (!state.State.IsInitialized)
            {
                throw new UserDoesNotExistException();
            }

            this.Reservations.Add(reservationId);

            await state.WriteStateAsync();
        }

        public Task<FullUserDetails> GetUserInfo()
        {
            logger.LogInformation($"Getting user info for {this.GetPrimaryKeyString()}");
            if (!state.State.IsInitialized)
            {
                throw new UserDoesNotExistException();
            }

            return Task.FromResult(new FullUserDetails(state.State, Reservations));
        }

        public Task<bool> IsInitialized()
        {
            return Task.FromResult(state.State.IsInitialized);
        }
    }

    [GenerateSerializer, Alias(nameof(UserDetails))]
    public sealed record class UserDetails
    {
        [Id(0)]
        public string Name { get; set; } = "";
        [Id(1)]
        public string Surname { get; set; } = "";
        [Id(2)]
        public bool IsInitialized { get; set; } = false;
    }
    [GenerateSerializer, Alias(nameof(FullUserDetails))]
    public sealed record class FullUserDetails
    {
        public FullUserDetails(UserDetails userDetails, List<string> reservations)
        {
            UserDetails = userDetails;
            Reservations = reservations;
        }
        [Id(0)]
        public UserDetails UserDetails { get; set; }
        [Id(1)]
        public List<string> Reservations { get; set; } = new();
    }
}
