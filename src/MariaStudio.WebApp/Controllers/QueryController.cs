using MariaStudio.WebApp.Models;
using MariaStudio.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace MariaStudio.WebApp.Controllers
{
    public sealed class QueryController(QueryService queries, SchemaService schema) : Controller
    {
        public IActionResult Index() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Execute([FromBody] QueryRequest? request, CancellationToken cancellationToken)
        {
            var result = await queries.ExecuteAsync(request?.Sql ?? "", cancellationToken);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> Tree(CancellationToken cancellationToken)
        {
            try
            {
                return Json(await schema.GetTreeAsync(cancellationToken));
            }
            catch (Exception ex) when (ex is MySqlException or InvalidOperationException)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Complete(string? prefix, CancellationToken cancellationToken)
        {
            try
            {
                return Json(await schema.CompleteAsync(prefix ?? "", cancellationToken));
            }
            catch (Exception ex) when (ex is MySqlException or InvalidOperationException)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}
