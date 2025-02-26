using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppTemplate.ApiServer.Database.Entities;

public class User
{
    public int Id { get; set; }

    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }

    [Column(TypeName="timestamp with time zone")]
    public DateTime InvalidateTimestamp { get; set; } = DateTime.MinValue;
}