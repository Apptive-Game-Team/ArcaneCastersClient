namespace Data
{
    public static class ServerList
    {
        /// <summary>
        /// Matching/lobby server currently in use. Admins can switch it at runtime from the lobby
        /// dropdown; see <see cref="MatchingServerCatalog"/> for the candidates and the default.
        /// </summary>
        public static Server MatchingServer => MatchingServerCatalog.Current.Server;

        /// <summary>
        /// Must sit in the same environment as <see cref="MatchingServer"/>. This server signs
        /// the session token and publishes the JWKS that both the lobby and
        /// <c>JwksService</c> verify it with, so a token from another environment is rejected.
        /// </summary>
        public static readonly Server AccountServer =
            new Server("magic-card", "account.magic-card.ac.theevilent.com", 443, true);
    }
}
