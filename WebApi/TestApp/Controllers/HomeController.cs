using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System;
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

        public object JsonConvert { get; private set; }

        public HomeController(ILogger<HomeController> logger, ITokenAcquisition tokenAcquisition, IDownstreamWebApi downstreamWebApi)
        {
            _logger = logger;
            this.tokenAcquisition = tokenAcquisition;
            this.downstreamWebApi = downstreamWebApi;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> Privacy()
        {
            //await CallApiUsingManualTokenAcquisition();
            await CallApiUsingHelper();

            return View();
        }

        private async Task CallApiUsingManualTokenAcquisition()
        {
            // Acquire the access token.
            string[] scopes = new string[] { "access_as_user" };
            string accessToken = await tokenAcquisition.GetAccessTokenForUserAsync(scopes); // MsalUiRequiredException

            // Use the access token to call a protected web API.
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.GetAsync($"https://localhost:5001/WeatherForecast");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsStringAsync();

                //dynamic me = JsonConvert.DeserializeObject(content);
                //ViewData["Me"] = me;
            }

            Console.WriteLine();
        }

        private async Task CallApiUsingHelper()
        {
            var value = await downstreamWebApi.CallWebApiForUserAsync(
                        "TestService",
                        options =>
                        {
                            options.RelativePath = $"WeatherForecast";
                        });

            Console.WriteLine();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
