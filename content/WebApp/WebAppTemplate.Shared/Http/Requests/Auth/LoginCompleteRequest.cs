using System.ComponentModel.DataAnnotations;

namespace WebAppTemplate.Shared.Http.Requests.Auth;

public class LoginCompleteRequest
{
    [Required(ErrorMessage = "You need to provide a code")]
    public string Code { get; set; }
}