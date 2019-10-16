using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using RestSharp;

namespace AcceptanceTests
{
    [TestFixture]
    [Category("Acceptance")]
    public class BasicTests
    {
        private readonly string _tempUrl = "https://APP_NAME HERE.azurewebsites.net/api/RepositoryValidator";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task BasicResponse()
        {
            var client = new RestClient();
            var request = new RestRequest(_tempUrl, Method.POST);
            request.AddQueryParameter("code", "codehere");
            request.AddJsonBody(new object { });

            var result = await client.ExecuteTaskAsync(request);

            Assert.AreEqual(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }
}