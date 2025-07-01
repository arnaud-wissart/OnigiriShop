namespace OnigiriShop.Infrastructure
{
    public static class AuthConfig
    {
        public static void AddOnigiriAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication("OnigiriAuth")
                .AddCookie("OnigiriAuth", options =>
                {
                    options.LoginPath = "/login";
                    options.Cookie.Name = "OnigiriShop.Auth";
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Always en prod !
                    options.Cookie.Path = "/";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                });

            services.AddAuthorizationCore();
        }
    }
}
