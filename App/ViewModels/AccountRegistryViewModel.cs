using System.ComponentModel.DataAnnotations;
namespace App.ViewModels
{
    public class AccountRegistryViewModel
    {
        [Required]
        public string Login { get; set; }
        [Required]
        public string Password { get; set; }
        public string surname { get; set; }
        public string name { get; set; }
        public string middlename { get; set; }
        public string email { get; set; }
    }
}