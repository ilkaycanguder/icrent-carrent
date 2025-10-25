using System.ComponentModel.DataAnnotations;

namespace ICRent.Web.Models.Account
{
    public class EditCredentialsViewModel
    {
        [Required, Display(Name = "Kullanıcı Adı")]
        [StringLength(50, MinimumLength = 3)]
        public string UserName { get; set; } = null!;

        [Required, Display(Name = "Mevcut Şifre")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = null!;

        [Required, Display(Name = "Yeni Şifre")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalı.")]
        public string NewPassword { get; set; } = null!;

        [Required, Display(Name = "Yeni Şifre (Tekrar)")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmNewPassword { get; set; } = null!;
    }
}
