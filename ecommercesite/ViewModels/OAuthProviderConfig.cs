namespace ecommercesite.ViewModels
{
    /// <summary>
    /// Configuration for a generic OAuth2 provider. Each entry in the
    /// <c>Authentication:Providers</c> array in <c>appsettings.json</c> is
    /// bound to one of these. This lets you plug in ANY OAuth2 provider
    /// (GitHub, Discord, LinkedIn, Spotify, Apple, GitLab, …) without
    /// writing any code — just fill in the endpoints + credentials.
    /// </summary>
    public class OAuthProviderConfig
    {
        /// <summary>Unique scheme name used as the authentication scheme
        /// (e.g. "GitHub"). Also passed as the <c>provider</c> query param
        /// to <c>AccountController.ExternalLogin</c>.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Human-readable label shown on the login button
        /// (e.g. "GitHub").</summary>
        public string DisplayName { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;

        /// <summary>OAuth2 authorization endpoint URL
        /// (where the user is redirected to consent).</summary>
        public string AuthorizationEndpoint { get; set; } = string.Empty;

        /// <summary>OAuth2 token endpoint URL
        /// (where the app exchanges the auth code for an access token).</summary>
        public string TokenEndpoint { get; set; } = string.Empty;

        /// <summary>URL the app calls with the access token to fetch the
        /// user's profile (must return JSON).</summary>
        public string UserInformationEndpoint { get; set; } = string.Empty;

        /// <summary>The JSON field name in the user-info response that
        /// contains the user's email address. Defaults to "email".
        /// For GitHub use "email". For Discord use "email". For GitLab
        /// use "email". For LinkedIn the email comes from a separate
        /// endpoint — leave blank and the app falls back to
        /// <c>preferred_username</c>.</summary>
        public string? EmailField { get; set; } = "email";

        /// <summary>OAuth2 scopes to request (e.g. "user:email" for GitHub,
        /// "identify email" for Discord).</summary>
        public List<string> Scopes { get; set; } = new();

        /// <summary>Brand color (hex) for the login-button badge.
        /// Defaults to a neutral gray.</summary>
        public string? Color { get; set; }
    }
}
