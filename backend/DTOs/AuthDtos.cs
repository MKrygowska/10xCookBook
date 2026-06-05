using System.ComponentModel.DataAnnotations;

namespace _10x_cookbook_backend.DTOs
{
    public record RegisterRequest(
        [Required(ErrorMessage = "E-mail jest wymagany.")]
        [EmailAddress(ErrorMessage = "Niepoprawny format e-mail.")]
        string Email,

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        [MinLength(6, ErrorMessage = "Hasło musi mieć co najmniej 6 znaków.")]
        string Password
    );

    public record LoginRequest(
        [Required(ErrorMessage = "E-mail jest wymagany.")]
        string Email,

        [Required(ErrorMessage = "Hasło jest wymagane.")]
        string Password
    );
}
