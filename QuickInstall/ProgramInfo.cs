using Newtonsoft.Json;

namespace QuickInstall
{
    class ProgramInfo
    {
        public required string Name { get; set; }
        public Dictionary<string, string> Architectures { get; set; } = new();
        public string? Arguments { get; set; }
        public List<string> Tags { get; set; } = new();
        [JsonIgnore]
        public bool IsSelected;
    }

}
