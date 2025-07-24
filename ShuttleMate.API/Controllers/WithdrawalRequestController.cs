using Microsoft.AspNetCore.Mvc;

namespace ShuttleMate.API.Controllers
{
    public class WithdrawalRequestController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
