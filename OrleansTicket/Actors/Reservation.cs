using Orleans.Runtime;
using OrleansTicket.Exception;

namespace OrleansTicket.Actors
{
    public interface IReservationGrain : IGrainWithGuidKey
    {
        Task<Guid> ReserveSeat(string eventId, string seatId, string email);
        Task<ReservationDetails> GetReservationInfo();
        Task CancelReservation();
        Task EventChangeAction();
        Task EventCancelledAction();
    }

    public sealed class ReservationGrain: Grain, IReservationGrain
    {
        private string Status { get; set; } = ReservationStates.CREATED;
        private IEventGrain Event { get; set; }
        private string SeatId { get; set; } = "";
        private IUserGrain User{ get; set; }
        private bool IsInitialized { get; set; } = false;
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
            RequestContext.Set("ReservationId", this.GetPrimaryKey());
            var success = await Event.CreateReservation(seatId);
            if (!success)
            {
                Status = ReservationStates.DECLINED;
                throw new ReservationDeclinedException();
            }

            Status = ReservationStates.ACTIVE;
            SeatId = seatId;
            IsInitialized = true;
            await User.AddReservation(this.GetPrimaryKeyString());
            return this.GetPrimaryKey();
        }

        public Task<ReservationDetails> GetReservationInfo()
        {
            if (!IsInitialized)
            {
                throw new ReservationDoesNotExistException();
            }
            return Task.FromResult(new ReservationDetails(this.GetPrimaryKey(), Status, Event.GetPrimaryKeyString(), SeatId));
        }

        public Task CancelReservation()
        {
            if (!IsInitialized)
            {
                throw new ReservationDoesNotExistException();
            }

            Status = ReservationStates.CANCELED;
            return Event.CancelReservation(SeatId!);
        }
        public Task EventChangeAction()
        {
            Console.WriteLine($"Event for reservation {this.GetPrimaryKeyString()} changed. Sending email to allow reservation changes to be made");
            return Task.CompletedTask;
        }
        public Task EventCancelledAction()
        {
            Status = ReservationStates.CANCELED;
            Console.WriteLine($"Reservation {this.GetPrimaryKeyString()} has been cancelled because of Event cancellation. Sending email with that information");
            return Task.CompletedTask;
        }
    }
    [GenerateSerializer, Alias(nameof(ReservationDetails))]
    public class ReservationDetails
    {
        public ReservationDetails(Guid reservationId, string status, string eventId, string seatId)
        {
            ReservationId = reservationId;
            Status = status;
            EventId = eventId;
            SeatId = seatId;
        }

        [Id(0)]
        public Guid ReservationId { get; }
        [Id(1)]
        public string Status { get; }
        [Id(2)]
        public string EventId { get; }
        [Id(3)]
        public string SeatId { get; }
    }
    public static class ReservationStates
    {
        public static readonly string CREATED = "CREATED";
        public static readonly string DECLINED = "DECLINED";
        public static readonly string ACTIVE = "ACTIVE";
        public static readonly string CANCELED = "CANCELED";
    }
}
