using OrleansTicket.Exception;

namespace OrleansTicket.Actors
{
    public interface IReservationGrain : IGrainWithGuidKey
    {
        Task<Guid> ReserveSeat(string eventId, string seatId, string email);
    }

    public sealed class ReservationGrain: Grain, IReservationGrain
    {
        private string Status { get; set; } = ReservationStates.CREATED;
        private IEventGrain Event { get; set; }
        private string SeatId { get; set; } = "";
        private IUserGrain User{ get; set; }
        public async Task<Guid> ReserveSeat(string eventId, string seatId, string email)
        {
            if (!Status.Equals(ReservationStates.CREATED))
            {
                throw new ReservationExistsException();
            }

            User = GrainFactory.GetGrain<IUserGrain>(email);
            if (!await User.IsInitialized())
            {
                Status = ReservationStates.DECLINED;
                throw new UserDoesNotExistException();
            }

            Event = GrainFactory.GetGrain<IEventGrain>(Guid.Parse(eventId));
            try
            {
                await Event.CreateReservation(seatId);
            }
            catch()
            {

            }
        }
    }
    public static class ReservationStates
    {
        public static readonly string CREATED = "CREATED";
        public static readonly string DECLINED = "DECLINED";
        public static readonly string ACTIVE = "ACTIVE";
        public static readonly string CANCELED = "CANCELED";
    }
}
