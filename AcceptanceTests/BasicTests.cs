using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using RestSharp;
using ValidationLibrary.AzureFunctions.GitHubDto;

namespace AcceptanceTests
{
    [TestFixture]
    public class BasicTests
    {
        private string _url;
        private string _code;

        [SetUp]
        public void Setup()
        {
            var name = TestContext.Parameters["FunctionAppName"];
            _code = TestContext.Parameters["FunctionAppCode"];
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(_code))
            {
                Assert.Ignore("Function app name or code not defined. Skipping acceptance tests.");
            }

            _url = $"https://{name}.azurewebsites.net/api/v1/github-endpoint";
        }

        [Test]
        public async Task RepositoryValidator_CorrectResponseForBadRequest()
        {
            var result = await SendRequest(new { });
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task RepositoryValidator_CorrectResponseForInvalidJsonObject()
        {
            var result = await SendRequest(new
            {
                repository = new
                {
                    name = "repository-validator-testing",
                    owner = "by-pinja"
                }
            });
            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Test]
        public async Task RepositoryValidator_CorrectResponseForCorrectRequest()
        {
            var data = new PushData()
            {
                Repository = new Repository
                {
                    Name = "repository-validator-testing",
                    Owner = new Owner
                    {
                        Login = "by-pinja"
                    }
                }
            };
            var result = await SendRequest(data);
            Assert.AreEqual(HttpStatusCode.Accepted, result.StatusCode);
        }

        private async Task<IRestResponse> SendRequest(object obj)
        {
            var client = new RestClient();
            var request = new RestRequest(_url, Method.POST);
            request.AddQueryParameter("code", _code);
            request.AddJsonBody(obj);

            return await client.ExecuteAsync(request);
        }
    }
}
