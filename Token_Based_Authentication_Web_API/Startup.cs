using Microsoft.Owin; using Owin;

[assembly: OwinStartup(typeof(Token_Based_Authentication_Web_API.Startup))]

namespace Token_Based_Authentication_Web_API
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
