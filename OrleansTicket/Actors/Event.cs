using OrleansTicket.Exception;
using System;

namespace OrleansTicket.Actors
{
    public interface IEventGrain : IGrainWithGuidKey
    {
        Task<Guid> InitializeEvent(string name, double duration, string location, DateTime date, List<CreateSeatData> seats);
        Task<FullEventDetails> UpdateEventInfo(string name, double duration, string location, DateTime date);
        Task<EventDetails> GetEventInfo();
        Task<FullEventDetails> GetFullEventInfo();
        Task CancelEvent();
        Task<bool> CreateReservation(string seatId);
        Task CancelReservation(string seatId);
    }
    public sealed class EventGrain: Grain, IEventGrain
    {
        private bool IsInitialized { get; set; } = false;
        private string Name { get; set; } = "";
        private double Duration { get; set; }
        private string Location { get; set; } = "";
        private DateTime Date { get; set; }
        private string Status { get; set; } = "";
        private List<Seat> _seatList = new();
        private Dictionary<string, IReservationGrain> _seatIdToReservation = new();
        public Task<Guid> InitializeEvent(string name, double duration, string location, DateTime date, List<CreateSeatData> seats)
        {
            if (IsInitialized)
            {
                throw new EventExistsException();
            }

            Name = name;
            Duration = duration;
            Location = location;
            Date = date;
            Status = EventStates.ACTIVE;
            seats.ForEach(seat => _seatList.Add(new Seat(Guid.NewGuid().ToString(), seat.Price)));

            return Task.FromResult(this.GetPrimaryKey());
        }

        public Task<EventDetails> GetEventInfo()
        {
            if (!IsInitialized)
            {
                throw new EventDoesNotExistException();
            }

            var availableSeats = _seatList.Where(seat => !_seatIdToReservation.ContainsKey(seat.Id)).ToList();
            return Task.FromResult(new EventDetails(Name, Duration, Location, Date, Status, _seatList.Count, availableSeats.Count, availableSeats));
        }

        public Task<FullEventDetails> GetFullEventInfo()
        {
            if (!IsInitialized)
            {
                throw new EventDoesNotExistException();
            }

            //availableSeats = _seatList.Where(seat => !seatIdToReservationActor.ContainsKey(seat.Id)).ToList();
            //var cheapestSeat = _seatList.Min(seat => seat.Price);
            //var router = Context.System.ActorSelection("/user/currencyExchangeRouter");
            //var sender = Sender;
            //cheapestSeat = await router.Ask<double>(new ExchangeCurrency(readMsg.Currency, cheapestSeat));
            //return Task.FromResult(new FullEventDetails(Name, Duration, Location, Date, Status, _seatList.Count, availableSeats.Count, availableSeats, cheapestSeat));
            var availableSeats = _seatList.Where(seat => !_seatIdToReservation.ContainsKey(seat.Id)).ToList();
            // TODO: Fix in akka
            var cheapestSeat = availableSeats.Min(seat => seat.Price);
            return Task.FromResult(new FullEventDetails(Name, Duration, Location, Date, Status, _seatList.Count, availableSeats.Count, availableSeats, cheapestSeat));
        }

        public Task<FullEventDetails> UpdateEventInfo(string name, double duration, string location, DateTime date)
        {
            if (!IsInitialized)
            {
                throw new EventDoesNotExistException();
            }

            Name = name;
            Duration = duration;
            Location = location;
            Date = date;
            var availableSeats = _seatList.Where(seat => !_seatIdToReservation.ContainsKey(seat.Id)).ToList();
            var cheapestSeat = availableSeats.Min(seat => seat.Price);
            //reservationActorToSeatId.Keys.ForEach(reservationActor => reservationActor.Tell(new EventChanged(requestChangeEventMsg.RequestId)));
            return Task.FromResult(new FullEventDetails(Name, Duration, Location, Date, Status, _seatList.Count, availableSeats.Count, availableSeats, cheapestSeat));
        }

        public Task CancelEvent()
        {
            if (!IsInitialized)
            {
                throw new EventDoesNotExistException();
            }

            this.Status = EventStates.CANCELED;
            //reservationActorToSeatId.Keys.ForEach(reservationActor => reservationActor.Tell(new EventCancelled(requestCancelEventMsg.RequestId)));
            _seatIdToReservation.Clear();
            return Task.CompletedTask;
        }

        public Task<bool> CreateReservation(string seatId)
        {
            if (!IsInitialized)
            {
                throw new EventDoesNotExistException();
            }

            // TODO: Create reservation
            if (_seatIdToReservation.ContainsKey(seatId))
            {
                return false;
            }


            //if (seatIdToReservationActor.ContainsKey(reservationRequestMsg.SeatId))
            //{
            //    Sender.Tell(new ReservationDeclination(reservationRequestMsg.RequestId));
            //}
            //else
            //{
            //    seatIdToReservationActor.Add(reservationRequestMsg.SeatId, reservationRequestMsg.ReservationActorRef);
            //    reservationActorToSeatId.Add(reservationRequestMsg.ReservationActorRef, reservationRequestMsg.SeatId);
            //    Context.Watch(reservationRequestMsg.ReservationActorRef);
            //    Sender.Tell(new ReservationConfirmation(reservationRequestMsg.RequestId, reservationRequestMsg.SeatId));
            //}
        }

        public Task CancelReservation(Guid seatId)
        {
            if (!IsInitialized)
            {
                throw new EventDoesNotExistException();
            }

            // TODO: Cancel reservation
            //if (reservationActorToSeatId.ContainsKey(Sender))
            //{
            //    var seatId = reservationActorToSeatId[Sender];
            //    seatIdToReservationActor.Remove(seatId);
            //    reservationActorToSeatId.Remove(Sender);
            //}
        }
    }
    public class Seat
    {
        public Seat(string id, double price)
        {
            Id = id;
            Price = price;
        }
        public string Id { get; set; }
        public double Price { get; set; }
    }
    public class CreateSeatData
    {
        public CreateSeatData(double price)
        {
            Price = price;
        }
        public double Price { get; set; }
    }
    public class EventDetails
    {
        public EventDetails(string name, double duration, string location, DateTime date, string status, int seatsAmount, int availableSeatsAmount, List<Seat> availableSeats)
        {
            Name = name;
            Duration = duration;
            Location = location;
            Date = date;
            Status = status;
            SeatsAmount = seatsAmount;
            AvailableSeatsAmount = availableSeatsAmount;
            AvailableSeats = availableSeats;
        }
        public string Name { get; }
        public double Duration { get; }
        public string Location { get; }
        public DateTime Date { get;}
        public string Status { get; }
        public int SeatsAmount { get; }
        public int AvailableSeatsAmount { get; }
        public List<Seat> AvailableSeats { get; }
    }
    public class FullEventDetails : EventDetails
    {
        public FullEventDetails(string name, double duration, string location, DateTime date, string status, int seatsAmount, int availableSeatsAmount, List<Seat> availableSeats, double cheapestSeat): base(name, duration, location, date, status, seatsAmount, availableSeatsAmount, availableSeats)
        {
            CheapestSeat = cheapestSeat;
        }
        public double CheapestSeat { get; }
    }
    public static class EventStates
    {
        public static readonly string ACTIVE = "ACTIVE";
        public static readonly string CANCELED = "CANCELED";
    }
}
