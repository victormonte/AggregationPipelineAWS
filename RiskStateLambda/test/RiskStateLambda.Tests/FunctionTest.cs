using Amazon.Lambda.KinesisEvents;
using Amazon.Lambda.TestUtilities;
using Xunit;

namespace RiskStateLambda.Tests
{
    public class FunctionTest
    {
        [Fact]
        public void TestFunction()
        {
            
            var context = new TestLambdaContext();
            var function = new Function();

            function.FunctionHandler(new KinesisEvent(), context);

            var testLogger = context.Logger as TestLambdaLogger;
			Assert.Contains("Stream processing complete", testLogger.Buffer.ToString());
        }  
    }
}
