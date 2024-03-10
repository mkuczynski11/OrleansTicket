using Orleans.Runtime;
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
            IsInitialized = true;

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
            var cheapestSeat = availableSeats.Count > 0 ? availableSeats.Min(seat => seat.Price) : 0;
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
            foreach (var item in _seatIdToReservation)
            {
                item.Value.EventChangeAction();
            }
            return Task.FromResult(new FullEventDetails(Name, Duration, Location, Date, Status, _seatList.Count, availableSeats.Count, availableSeats, cheapestSeat));
        }

        public Task CancelEvent()
        {
            if (!IsInitialized)
            {
                throw new EventDoesNotExistException();
            }

            this.Status = EventStates.CANCELED;
            foreach (var item in _seatIdToReservation)
            {
                item.Value.EventCancelledAction();
            }
            _seatIdToReservation.Clear();
            return Task.CompletedTask;
        }

        public Task<bool> CreateReservation(string seatId)
        {
            if (!IsInitialized)
            {
                throw new EventDoesNotExistException();
            }

            if (_seatList.Find(seat => seat.Id.Equals(seatId)) == null)
            {
                throw new SeatDoesNotExistException();
            }

            if (_seatIdToReservation.ContainsKey(seatId))
            {
                return Task.FromResult(false);
            }

            var reservationId = RequestContext.Get("ReservationId") as Guid?;
            if (!reservationId.HasValue)
            {
                throw new InvalidCastException();
            }

            var reservationGrain = GrainFactory.GetGrain<IReservationGrain>(reservationId.Value);
            _seatIdToReservation.Add(seatId, reservationGrain);
            return Task.FromResult(true);
        }

        public Task CancelReservation(string seatId)
        {
            if (!IsInitialized)
            {
                throw new EventDoesNotExistException();
            }

            if (_seatIdToReservation.ContainsKey(seatId))
            {
                _seatIdToReservation.Remove(seatId);
            }

            return Task.CompletedTask;
        }
    }
    [GenerateSerializer, Alias(nameof(Seat))]
    public class Seat
    {
        public Seat(string id, double price)
        {
            Id = id;
            Price = price;
        }

        [Id(0)]
        public string Id { get; set; }
        [Id(1)]
        public double Price { get; set; }
    }
    [GenerateSerializer, Alias(nameof(CreateSeatData))]
    public class CreateSeatData
    {
        public CreateSeatData(double price)
        {
            Price = price;
        }

        [Id(0)]
        public double Price { get; set; }
    }
    [GenerateSerializer, Alias(nameof(EventDetails))]
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

        [Id(0)]
        public string Name { get; }
        [Id(1)]
        public double Duration { get; }
        [Id(2)]
        public string Location { get; }
        [Id(3)]
        public DateTime Date { get;}
        [Id(4)]
        public string Status { get; }
        [Id(5)]
        public int SeatsAmount { get; }
        [Id(6)]
        public int AvailableSeatsAmount { get; }
        [Id(7)]
        public List<Seat> AvailableSeats { get; }
    }
    [GenerateSerializer, Alias(nameof(FullEventDetails))]
    public class FullEventDetails : EventDetails
    {
        public FullEventDetails(string name, double duration, string location, DateTime date, string status, int seatsAmount, int availableSeatsAmount, List<Seat> availableSeats, double cheapestSeat): base(name, duration, location, date, status, seatsAmount, availableSeatsAmount, availableSeats)
        {
            CheapestSeat = cheapestSeat;
        }

        [Id(8)]
        public double CheapestSeat { get; }
    }
    public static class EventStates
    {
        public static readonly string ACTIVE = "ACTIVE";
        public static readonly string CANCELED = "CANCELED";
    }
}
