using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TestAppForGraph.Models;

namespace TestAppForGraph.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly GraphServiceClient graphServiceClient;

        private readonly MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler;

        private string[] graphScopes = new[] { "user.read" };


        public HomeController(ILogger<HomeController> logger,
                              GraphServiceClient graphServiceClient,
                              MicrosoftIdentityConsentAndConditionalAccessHandler consentHandler,
                              IConfiguration configuration)
        {
            _logger = logger;
            this.graphServiceClient = graphServiceClient;
            this.consentHandler = consentHandler;

            // Capture the Scopes for Graph that were used in the original request for an Access token (AT) for MS Graph as
            // they'd be needed again when requesting a fresh AT for Graph during claims challenge processing
            graphScopes = configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
        }

        public IActionResult Index()
        {
            return View();
        }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<IActionResult> Privacy()
        {
            User currentUser = null;

            try
            {
                currentUser = await graphServiceClient.Me.Request().GetAsync();
            }
            // Catch CAE exception from Graph SDK
            catch (ServiceException svcex) when (svcex.Message.Contains("Continuous access evaluation resulted in claims challenge"))
            {
                try
                {
                    Console.WriteLine($"{svcex}");
                    var claimChallenge = AuthenticationHeaderHelper.ExtractClaimChallengeFromHttpHeader(svcex.ResponseHeaders);
                    consentHandler.ChallengeUser(graphScopes, claimChallenge);
                    return new EmptyResult();
                }
                catch (Exception ex2)
                {
                    consentHandler.HandleException(ex2);
                }
            }

            try
            {
                // Get user photo
                using (var photoStream = await graphServiceClient.Me.Photo.Content.Request().GetAsync())
                {
                    byte[] photoByte = ((MemoryStream)photoStream).ToArray();
                    ViewData["Photo"] = Convert.ToBase64String(photoByte);
                }
            }
            catch (Exception pex)
            {
                Console.WriteLine($"{pex.Message}");
                ViewData["Photo"] = null;
            }

            ViewData["Me"] = currentUser;
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
