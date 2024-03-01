namespace OrleansTicket.DTO
{
    public sealed class CreateEventSeatDTO
    {
        public CreateEventSeatDTO(double price) 
        {
            Price = price;
        }
        public double Price { get; }
    }
}
