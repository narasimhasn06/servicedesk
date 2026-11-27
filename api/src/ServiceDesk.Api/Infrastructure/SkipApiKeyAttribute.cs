namespace ServiceDesk.Api.Infrastructure;

// For callers that can't send X-Api-Key (payment-provider webhooks).
// Such endpoints MUST authenticate the request another way, e.g. a signature.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SkipApiKeyAttribute : Attribute;