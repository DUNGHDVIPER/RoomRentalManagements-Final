namespace WebAdmin.MVC.Models.Rooms
{
    public class RoomDetailsVm
    {
        public int Id { get; set; }

        public string RoomCode { get; set; }

        public string RoomName { get; set; }

        public string Block { get; set; }

        public string Floor { get; set; }

        public decimal Price { get; set; }

        public string Status { get; set; }

        public List<RoomImageVm> Images { get; set; } = new();
        public List<string> Amenities { get; set; } = new();
    }
}