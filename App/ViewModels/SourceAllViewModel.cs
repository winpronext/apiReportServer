namespace App.ViewModels
{
    public class SourceAllViewModel
    {
        public int id { get; set; }

        public string name { get; set; }
        public string server { get; set; }
        public string db { get; set; }
        public string login { get; set; }
        public TypeSourceViewModel typeSource { get; set; }
    }
}