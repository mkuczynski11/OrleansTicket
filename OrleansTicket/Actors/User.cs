using Orleans;
using Orleans.Runtime;
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
    public sealed class UserGrain(
        [PersistentState(stateName: "user", storageName: "users")] IPersistentState<UserDetails> state): Grain, IUserGrain
    {
        private List<string> Reservations { get; set; } = new();
        public async Task<string> InitializeUser(string name, string surname)
        {
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
            if (!state.State.IsInitialized)
            {
                throw new UserDoesNotExistException();
            }

            this.Reservations.Add(reservationId);

            await state.WriteStateAsync();
        }

        public Task<FullUserDetails> GetUserInfo()
        {
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
