namespace IoT.Gateway.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication SetUpMiddleWare(this WebApplication webApp)
    {
        if (webApp.Environment.IsDevelopment())
        {
            webApp.UseSwagger();
            webApp.UseSwaggerUI();
        }

        // Check if running in a container
        var inContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

        if (!inContainer)
        {
            webApp.UseHttpsRedirection();
        }


        webApp.RegisterEndpoints();

        return webApp;
    }
}