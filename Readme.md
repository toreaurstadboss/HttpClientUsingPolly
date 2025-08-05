# HttpClient Using Polly v9 demo V2

This is a simple demo using Asp.net Core and Polly v9 with Simmy. 

## What is this demo?
This Asp.net core repo contains code to get started with Polly v9. The solution uses .NET 8 as target framework. 
Simmy is used to inject faults and instability using Simmy, a library to use with Polly to inject 'chaos engine' and 
therefore test out stability / resilience of services in Asp.net Core in this demo.

### What is possible with the demo ?

#### Using Polly V9 with IHttpClientFactory created http client
The demo shows how Polly v9 can be used together with `IHttpClientFactory` to define http client which supports 
retry policy. This makes the amount of boilerplate to write in the endpoints shorter. 

Examples of this can be seen in the `-v2` methods inside `GithubEndpoints.cs`.

#### Using Polly V9 with ResiliencePipelineProvider and registered resilience pipeline
A longer variant is to make use of `ResiliencePipelineProvider<string>` injection and retrieving a 
resilience pipeline which is set up in `PollyExtensions.cs` inside method `AddNamedPollyPipelines` of this class.

Usage of this is seen in the `-v1` methods inside `GithubEndpoints.cs`.

#### Setting up certificates to run the demo
Run these commands to trust the setup Dotnet Dev-certs

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust

```