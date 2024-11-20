using Newtonsoft.Json;

namespace QuickInstall
{
    public class ProgramInfo
    {
        /// <summary>
        /// Name of the program.
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Dictionary of download links for different architectures.
        /// </summary>
        [JsonProperty("architectures")]
        public Dictionary<string, string> Architectures { get; set; } = new();

        /// <summary>
        /// Optional arguments to pass during installation.
        /// </summary>
        [JsonProperty("arguments")]
        public string? Arguments { get; set; }

        /// <summary>
        /// List of tags associated with the program (e.g., categories, features).
        /// </summary>
        [JsonProperty("tags")]
        public List<string> Tags { get; set; } = new();

        /// <summary>
        /// Indicates whether the program is currently selected for installation. Not serialized to JSON.
        /// </summary>
        [JsonIgnore]
        public bool IsSelected { get; set; }

        /// <summary>
        /// Validates the object's state and ensures required properties are set.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("Program name cannot be null or empty.");

            if (Architectures == null || Architectures.Count == 0)
                throw new InvalidOperationException("At least one architecture must be specified.");
        }

        /// <summary>
        /// Provides a string representation of the program's information.
        /// </summary>
        /// <returns>A readable summary of the program info.</returns>
        public override string ToString()
        {
            string architectures = Architectures.Count > 0
                ? string.Join(", ", Architectures.Keys)
                : "None";

            string tags = Tags.Count > 0
                ? string.Join(", ", Tags)
                : "None";

            return $"Name: {Name}, Architectures: [{architectures}], Tags: [{tags}], IsSelected: {IsSelected}";
        }
    }
}
