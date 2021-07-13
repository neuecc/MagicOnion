using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Benchmark.Server.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SumController : ControllerBase
    {
        private readonly ILogger<SumController> _logger;

        public SumController(ILogger<SumController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public Task<int> Post(int x, int y)
        {
            Statistics.Connected();
            try
            {
                return Task.FromResult(x + y);
            }
            catch
            {
                Statistics.Error();
                return Task.FromResult(-1);
            }
            finally
            {
                Statistics.Disconnected();
            }
        }
    }
}
