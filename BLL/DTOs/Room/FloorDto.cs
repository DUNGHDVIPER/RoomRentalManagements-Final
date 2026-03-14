namespace BLL.DTOs.Property
{
    public class FloorDto
    {
        public int Id { get; set; }

        public int BlockId { get; set; }

        public string FloorName { get; set; }

        public int TotalRooms { get; set; }   // thêm dòng này
    }
}   