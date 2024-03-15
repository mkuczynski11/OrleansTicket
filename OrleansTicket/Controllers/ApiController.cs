using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OrleansTicket.Actors;
using OrleansTicket.DTO;
using OrleansTicket.Exception;

namespace OrleansTicket.Controllers
{
    [Route("/api")]
    [ApiController]
    public class ApiController
    {
        private readonly ILogger<ApiController> _logger;
        private readonly IGrainFactory _grainFactory;

        public ApiController(ILogger<ApiController> logger, IGrainFactory grainFactory)
        {
            _logger = logger;
            _grainFactory = grainFactory;
        }

        [HttpGet("users/{email}")]
        public async Task<Results<NotFound<string>, Ok<GetUserDataDTO>>> GetUserData(string email)
        {
            var userGrain = _grainFactory.GetGrain<IUserGrain>(email);

            try
            {
                var userInfo = await userGrain.GetUserInfo();
                return TypedResults.Ok(new GetUserDataDTO(userGrain.GetPrimaryKeyString(), userInfo.UserDetails.Name, userInfo.UserDetails.Surname, userInfo.Reservations));
            }
            catch (UserDoesNotExistException e) 
            {
                return TypedResults.NotFound("User does not exist");
            }
        }

        [HttpPost("users")]
        public async Task<Results<BadRequest<string>, Created>> CreateUser([FromBody] CreateUserDTO createUserRequest)
        {
            var userGrain = _grainFactory.GetGrain<IUserGrain>(createUserRequest.Email);
            try
            {
                var userEmail = await userGrain.InitializeUser(createUserRequest.Name, createUserRequest.Surname);
                return TypedResults.Created(userEmail);
            }
            catch(UserExistsException e)
            {
                return TypedResults.BadRequest("User already exists");
            }
        }

        [HttpPut("users/{email}")]
        public async Task<Results<NotFound<string>, Ok>> ChangeUser(string email, [FromBody] UpdateUserDTO updateUserRequest)
        {
            var userGrain = _grainFactory.GetGrain<IUserGrain>(email);
            try
            {
                await userGrain.UpdateUserInfo(updateUserRequest.Name, updateUserRequest.Surname);
                return TypedResults.Ok();
            }
            catch (UserDoesNotExistException e)
            {
                return TypedResults.NotFound("User does not exist");
            }
        }

        [HttpGet("events/{id}")]
        public async Task<Results<NotFound<string>, Ok<GetEventDataDTO>>> GetEvent(string id, string currency)
        {
            var eventGrain = _grainFactory.GetGrain<IEventGrain>(Guid.Parse(id));
            try
            {
                var eventInfo = await eventGrain.GetFullEventInfo(currency);
                var seatsData = eventInfo.AvailableSeats.Select(seat => new GetEventSeatDTO(seat.Id, seat.Price)).ToList();
                return TypedResults.Ok(new GetEventDataDTO(eventGrain.GetPrimaryKeyString(), eventInfo.Name, eventInfo.Duration, eventInfo.Location, eventInfo.Date, eventInfo.Status, eventInfo.SeatsAmount, eventInfo.AvailableSeatsAmount, seatsData, eventInfo.CheapestSeat));
            }
            catch (EventDoesNotExistException e)
            {
                // TODO: Fix in akka
                return TypedResults.NotFound("Event does not exist");
            }
        }

        [HttpPost("events")]
        public async Task<Created> CreateEvent([FromBody] CreateEventDTO createEventRequest)
        {
            var eventGrain = _grainFactory.GetGrain<IEventGrain>(Guid.NewGuid());
            List<CreateSeatData> seats = createEventRequest.Seats.Select(seat => new CreateSeatData(seat.Price)).ToList();
            var eventId = await eventGrain.InitializeEvent(createEventRequest.Name, createEventRequest.Duration, createEventRequest.Location, createEventRequest.Date, seats);

            return TypedResults.Created(eventId.ToString());
        }

        [HttpDelete("events/{id}")]
        public async Task<Results<NotFound<string>, NoContent>> CancelEvent(string id)
        {
            var eventGrain = _grainFactory.GetGrain<IEventGrain>(Guid.Parse(id));
            try
            {
                await eventGrain.CancelEvent();
                return TypedResults.NoContent();
            }
            catch (EventDoesNotExistException e)
            {
                return TypedResults.NotFound("Event does not exist");
            }
        }

        [HttpPut("events/{id}")]
        public async Task<Results<NotFound<string>, Ok>> ChangeEvent(string id, [FromBody] UpdateEventDTO updateEventRequest)
        {
            var eventGrain = _grainFactory.GetGrain<IEventGrain>(Guid.Parse(id));
            try
            {
                var eventInfo = await eventGrain.UpdateEventInfo(updateEventRequest.Name, updateEventRequest.Duration, updateEventRequest.Location, updateEventRequest.Date);
                return TypedResults.Ok();
            }
            catch (EventDoesNotExistException e)
            {
                return TypedResults.NotFound("Event does not exist");
            }
        }

        [HttpGet("events")]
        public async Task<Ok<GetEventsDataDTO>> GetEvents(string? name)
        {
            var eventQueryGrain = _grainFactory.GetGrain<IEventQueryGrain>(Guid.NewGuid());
            var eventInfos = await eventQueryGrain.GetAllEvents(name);
            var eventList = eventInfos.Select(x => new GetEventsDataDTO.EventDTO(x.Id, x.Name)).ToList();

            return TypedResults.Ok(new GetEventsDataDTO(eventList));
        }

        [HttpPost("events/{eventId}/seats/{seatId}")]
        public async Task<Results<NotFound<string>, BadRequest<string>, Created>> ReserveSeat([FromBody] CreateReservationDTO createReservationRequest, string eventId, string seatId)
        {
            var reservationGrain = _grainFactory.GetGrain<IReservationGrain>(Guid.NewGuid());
            try
            {
                var reservationId = await reservationGrain.ReserveSeat(eventId, seatId, createReservationRequest.Email);
                return TypedResults.Created(reservationId.ToString());
            }
            catch (ReservationExistsException e)
            {
                return TypedResults.NotFound("Reservation already exists");
            }
            catch (EventDoesNotExistException e)
            {
                return TypedResults.NotFound("Event does not exist");
            }
            catch (UserDoesNotExistException e)
            {
                return TypedResults.NotFound("User does not exist");
            }
            catch (SeatDoesNotExistException e)
            {
                return TypedResults.NotFound("Seat does not exist");
            }
            catch (ReservationDeclinedException e)
            {
                return TypedResults.BadRequest("Seat is already reserved");
            }
        }

        [HttpDelete("reservations/{id}")]
        public async Task<Results<NotFound<string>, NoContent>> CancelReservation(string id)
        {
            var reservationGrain = _grainFactory.GetGrain<IReservationGrain>(Guid.Parse(id));
            try
            {
                await reservationGrain.CancelReservation();
                return TypedResults.NoContent();
            }
            catch (ReservationDoesNotExistException e)
            {
                return TypedResults.NotFound("Reservation does not exist");
            }
        }

        [HttpGet("reservations/{id}")]
        public async Task<Results<NotFound<string>, Ok<GetReservationDataDTO>>> GetReservation(string id)
        {
            var reservationGrain = _grainFactory.GetGrain<IReservationGrain>(Guid.Parse(id));
            try
            {
                var reservationInfo = await reservationGrain.GetReservationInfo();
                return TypedResults.Ok(new GetReservationDataDTO(reservationInfo.ReservationId.ToString(), reservationInfo.Status, reservationInfo.EventId, reservationInfo.SeatId));
            }
            catch(ReservationDoesNotExistException e)
            {
                return TypedResults.NotFound("Reservation does not exist");
            }
        }
    }
}
