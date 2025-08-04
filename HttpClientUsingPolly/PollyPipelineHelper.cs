using Polly.Registry;

namespace HttpClientUsingPolly
{
    public static class PollyPipelineHelper
    {

        public static ValueTask<HttpResponseMessage> ExecuteWithPolicyAsync(
            ResiliencePipelineProvider<string> pipelineProvider,
            string policyName,
            Func<CancellationToken, Task<HttpResponseMessage>> action,
            CancellationToken cancellationToken = default)
        {
            var pipeline = pipelineProvider.GetPipeline(policyName);

            return pipeline.ExecuteAsync(
                async ct => await action(ct),
                cancellationToken);
        }

    }
}
