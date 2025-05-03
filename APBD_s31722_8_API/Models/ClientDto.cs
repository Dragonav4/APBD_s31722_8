using System.ComponentModel.DataAnnotations;

namespace APBD_s31722_8_API.Models;

public class ClientDto
{
    public int Id { get; set; }
    [Required, StringLength(120)]
    public string FirstName { get; set; }
    [Required, StringLength(120)]
    public string LastName { get; set; }
    [Required, StringLength(120)]
    public string Email { get; set; }
    [Required, StringLength(120)]
    public string Telephone { get; set; }
    [Required, StringLength(120)]
    public string Pesel { get; set; }
    
    public ClientDto() {} 
}