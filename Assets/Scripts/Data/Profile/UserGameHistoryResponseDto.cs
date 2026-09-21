using System;

namespace Data.Profile
{
    [Serializable]
    public class UserGameHistoryResponseDto
    {
        public UserGameHistoryDto[] games;
        public UserGameHistoryDto[] content;
        public int number;
        public int page;
        public int size;
        public int totalPages;
        public bool last;

        public UserGameHistoryDto[] Items => games ?? content ?? Array.Empty<UserGameHistoryDto>();

        // The server now always sends an explicit last (page, size, totalPages, last —
        // ArcaneCastersLobby#27), so this is a direct read, not a guess from page length.
        public bool IsLastPage => last;
    }
}
