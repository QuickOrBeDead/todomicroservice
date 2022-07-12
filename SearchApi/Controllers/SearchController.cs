namespace SearchApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;

    using Nest;

    [ApiController]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly IElasticClient _elasticClient;

        public SearchController(IElasticClient elasticClient)
        {
            _elasticClient = elasticClient;
        }

        [HttpGet(Name = "GetAll")]
        public IEnumerable<Infrastructure.Model.Task> GetAll()
        {
            var searchResponse = _elasticClient.Search<Infrastructure.Model.Task>(s => s
                .Query(q => q
                    .MatchAll()
                )
            );

            return searchResponse.Documents;
        }
    }
}