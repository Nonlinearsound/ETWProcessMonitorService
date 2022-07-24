
namespace App.WindowsService
{
    public enum Exporttype
    {
        Logfile,
        Mail,
        Slack
    }

    public class ETWConfigItem
    {
        public string Name { get; set; }
        public string Provider { get; set;}
        public string Query { get; set; }
        public Exporttype Exporttype { get; set; }
    }

    public class ETWConfig
    {
        public ETWConfigItem[] ConfigItems { get; set; }
    }
}
