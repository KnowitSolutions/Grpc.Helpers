# gRPC Message Validator

This library implements a gRPC interceptor that will automatically validate request messages for all gRPC endpoints. Validators must be implemented using the [FluentValidation](https://fluentvalidation.net/) library.

## Getting started

1. Add this library to your gRPC Service
2. Install the [FluentValidation](https://www.nuget.org/packages/FluentValidation) NuGet package
3. Install the [FluentValidation.AspNetCore](https://www.nuget.org/packages/FluentValidation.AspNetCore) NuGet package
4. Add FluentValidation in `ConfigureServices`

```cs
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddMvc().AddFluentValidation();
    services.AddGrpc(options => options.AddValidationInterceptor());
    ...
}
```

## Creating validators

Read the [FluentValidation docs](https://fluentvalidation.net/start#creating-your-first-validator) to learn how to create validators.

In order for the validation interceptor to gain access to your validators, they must be added to the service collection. This library provides a convenience method for this purpose

```cs 
services.AddValidator<MyValidator>();
```

This is equivalent to

```
services.AddTransient<IValidator<MyClass>, MyValidator>();
```

where `MyValidator` is a validator for `MyClass`.

You can also add validators using the FluentValidation ASP.NET integration. See [the documentation](https://fluentvalidation.net/aspnet#getting-started).


## How it works

When a gRPC request that takes a `TRequest` is received, the interceptor will look for an implementation of `IValidator<TRequest>` and if found, will perform a validation on the message. If no validators are found, a warning is logged.
