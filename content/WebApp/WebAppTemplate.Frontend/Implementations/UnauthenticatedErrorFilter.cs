using Microsoft.AspNetCore.Components;
using MoonCore.Blazor.FlyonUi.Exceptions;
using MoonCore.Exceptions;

namespace WebAppTemplate.Frontend.Implementations;

public class UnauthenticatedErrorFilter : IGlobalErrorFilter
{
    private readonly NavigationManager Navigation;

    public UnauthenticatedErrorFilter(NavigationManager navigation)
    {
        Navigation = navigation;
    }

    public Task<bool> HandleExceptionAsync(Exception ex)
    {
        if(ex is not HttpApiException { Status: 401 })
            return Task.FromResult(false);
        
        Navigation.NavigateTo("/api/auth/logout", true);
        return Task.FromResult(true);
    }
}