using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using RestSharp;

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
                Assert.Inconclusive("Function app name or code not defined. Skipping acceptance tests.");
            }

            _url = $"https://{name}.azurewebsites.net/api/RepositoryValidator";
        }

        [Test]
        public async Task RepositoryValidator_CorrectResponseForBadRequest()
        {
            var client = new RestClient();
            var request = new RestRequest(_url, Method.POST);
            request.AddQueryParameter("code", _code);
            request.AddJsonBody(new object { });

            var result = await client.ExecuteTaskAsync(request);

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }
}