using System.ComponentModel.DataAnnotations;
namespace App.ViewModels
{
    public class DirectoryAddModel
    {
        [Required]
        public string directoryName { get; set; }

        [Required]
        public string directoryPrefics { get; set; }
    }
}