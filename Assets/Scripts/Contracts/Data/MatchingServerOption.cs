namespace Data
{
    /// <summary>
    /// One selectable matching server target shown in the admin server dropdown.
    /// </summary>
    public class MatchingServerOption
    {
        public MatchingServerOption(string key, string displayName, Server server)
        {
            Key = key;
            DisplayName = displayName;
            Server = server;
        }

        /// <summary>Stable identifier used to persist the admin's choice.</summary>
        public string Key { get; }

        /// <summary>Label rendered in the dropdown.</summary>
        public string DisplayName { get; }

        public Server Server { get; }
    }
}
