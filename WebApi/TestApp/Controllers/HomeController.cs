using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TestApp.Models;

namespace TestApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ITokenAcquisition tokenAcquisition;
        private readonly IDownstreamWebApi downstreamWebApi;

        private readonly string[] scopes = new[] { "user.read" };

        public HomeController(ILogger<HomeController> logger,
                              ITokenAcquisition tokenAcquisition,
                              IDownstreamWebApi downstreamWebApi,
                              IConfiguration configuration)
        {
            _logger = logger;
            this.tokenAcquisition = tokenAcquisition;
            this.downstreamWebApi = downstreamWebApi;

            scopes = configuration.GetValue<string>("TestService:Scopes")?.Split(' ');
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Privacy()
        {
            //var result = await CallApiUsingManualTokenAcquisition();
            var result = await CallApiUsingHelper();

            return View(result);
        }

        private async Task<IEnumerable<WeatherForecast>> CallApiUsingManualTokenAcquisition()
        {
            // Acquire the access token.
            string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes); // MsalUiRequiredException
            // MsalUiRequiredException: No account or login hint was passed to the AcquireTokenSilent call.

            // Use the access token to call a protected web API.
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://localhost:5001/WeatherForecast");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();

                var forcast = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(content);
                return forcast;
            }

            return null;
        }

        private async Task<IEnumerable<WeatherForecast>> CallApiUsingHelper()
        {
            var response = await downstreamWebApi.CallWebApiForUserAsync(
                        "TestService",
                        options =>
                        {
                            options.RelativePath = $"WeatherForecast";
                        });

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();

                var forcast = JsonConvert.DeserializeObject<IEnumerable<WeatherForecast>>(content);
                return forcast;
            }

            return null;
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
