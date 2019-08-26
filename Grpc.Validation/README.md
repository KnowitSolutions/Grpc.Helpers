# gRPC Message Validator

This library implements a gRPC interceptor that will automatically validate request messages for all gRPC endpoints. Validators must be implemented using the [FluentValidation](https://fluentvalidation.net/) library.

## Getting started

1. Add this library to your gRPC Service
2. Add the validation interceptor in `ConfigureServices`

```cs
public void ConfigureServices(IServiceCollection services)
{
    ...
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

You can also add validators using the FluentValidation ASP.NET integration. See [the documentation](https://fluentvalidation.net/aspnet#getting-started).


## How it works

When a gRPC request that takes a `TRequest` is received, the interceptor will look for an implementation of `IValidator<TRequest>` and if found, will perform a validation on the message. If no validators are found, a warning is logged.
