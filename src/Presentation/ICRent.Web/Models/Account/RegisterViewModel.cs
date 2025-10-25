using System.ComponentModel.DataAnnotations;

namespace ICRent.Web.Models.Account
{
    public class RegisterViewModel
    {
        [Required, Display(Name = "Kullanıcı Adı")]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = "";

        [Required, Display(Name = "Şifre")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
        public string Password { get; set; } = "";

    }
}
