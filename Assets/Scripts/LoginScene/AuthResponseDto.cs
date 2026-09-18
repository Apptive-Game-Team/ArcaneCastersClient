using System;

namespace LoginScene
{
    [Serializable]
    public class AuthResponseDto
    {
        public string jwt;

        /// <summary>Body 전달 방식에서만 온다. Cookie 방식은 이 필드가 비어 있다.</summary>
        public string refreshToken;

        /// <summary>access token 이 만료될 때까지 남은 초. 전달 방식과 관계없이 항상 온다.</summary>
        public int expiresIn;
    }
}
