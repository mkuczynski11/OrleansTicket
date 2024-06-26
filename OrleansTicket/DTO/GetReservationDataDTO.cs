﻿namespace OrleansTicket.DTO
{
    public sealed class GetReservationDataDTO
    {
        public GetReservationDataDTO(string id, string status, string eventId, string seatId)
        {
            Id = id;
            Status = status;
            EventId = eventId;
            SeatId = seatId;
        }
        public string Id { get; }
        public string Status { get; }
        public string EventId { get; }
        public string SeatId { get; }
    }
}
