using System.ComponentModel.DataAnnotations;
namespace App.ViewModels
{
    public class Report
    {
        [Required]
        public int id { get; set; }

        [Required]
        public string query { get; set; }

        [Required]
        public SourceViewModel source { get; set; }

        [Required]
        public TypeSourceViewModel sourceType { get; set; }
    }
}