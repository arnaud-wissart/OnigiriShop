namespace OnigiriShop.Infrastructure
{
    public static class AuthConfig
    {
        public static void AddOnigiriAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(AuthConstants.Scheme)
                .AddCookie(AuthConstants.Scheme, options =>
                {
                    options.Cookie.Name = AuthConstants.CookieName;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Always 'Always' en prod !
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.Path = "/";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                });

            services.AddAuthorizationCore();
        }
    }
}
