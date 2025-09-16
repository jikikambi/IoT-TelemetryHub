using Grpc.Core;
using Grpc.Core.Interceptors;

namespace IoT.DeviceApp.Application.Services;

public class JwtInterceptor(Func<string> getToken) : Interceptor
{

    private Metadata GetAuthHeaders()
    {
        var token = getToken();
        var metadata = new Metadata();
        if (!string.IsNullOrWhiteSpace(token))
        {
            metadata.Add("Authorization", $"Bearer {token}");
        }
        return metadata;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = GetAuthHeaders();
        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);
        return base.AsyncUnaryCall(request, newContext, continuation);
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = GetAuthHeaders();
        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);
        return base.AsyncServerStreamingCall(request, newContext, continuation);
    }

    public override AsyncClientStreamingCall<TRequest, TResponse> AsyncClientStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncClientStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = GetAuthHeaders();
        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);
        return base.AsyncClientStreamingCall(newContext, continuation);
    }

    public override AsyncDuplexStreamingCall<TRequest, TResponse> AsyncDuplexStreamingCall<TRequest, TResponse>(
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncDuplexStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var headers = GetAuthHeaders();
        var newOptions = context.Options.WithHeaders(headers);
        var newContext = new ClientInterceptorContext<TRequest, TResponse>(
            context.Method, context.Host, newOptions);
        return base.AsyncDuplexStreamingCall(newContext, continuation);
    }
}