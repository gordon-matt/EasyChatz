using Microsoft.AspNetCore.Mvc;

namespace EasyChatz.Demo.Mvc.Controllers;

public sealed class HomeController : Controller
{
    [HttpGet("/")]
    public IActionResult Index() => View();

    [HttpGet("/error")]
    public IActionResult Error() => Problem();
}
