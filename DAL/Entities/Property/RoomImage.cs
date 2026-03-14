namespace DAL.Entities.Property
{
    public class RoomImage
    {
        public int ImageId { get; set; }

        public int RoomId { get; set; }

        public string ImageUrl { get; set; }

        public bool IsPrimary { get; set; }

        public DateTime CreatedAt { get; set; }

        public Room Room { get; set; }
    }
}