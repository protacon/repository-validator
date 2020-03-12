using Microsoft.Extensions.Logging;
using NSubstitute;
using ValidationLibrary.Rules;

namespace ValidationLibrary.Tests.Rules
{
    public class HasDescriptionRuleTests : BaseRuleTests<HasDescriptionRule>
    {
        protected override void OnSetup()
        {
            _rule = new HasDescriptionRule(Substitute.For<ILogger<HasDescriptionRule>>());
        }
    }
}
