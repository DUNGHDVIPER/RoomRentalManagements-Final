using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.DTOs.Room.Public;
public class BookingCreateDTO
{
    [Required]
    public int RoomId { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }
}