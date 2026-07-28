using System.Text;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapMethods(
    "/{**path}",
    new[]
    {
        HttpMethods.Get,
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
        HttpMethods.Head,
        HttpMethods.Options
    },
    async (HttpContext context) =>
    {
        var request = context.Request;

        Console.WriteLine(
            $"[{DateTimeOffset.Now:O}] " +
            $"{request.Method} {request.Path}{request.QueryString}");

        Console.WriteLine("--- Headers ---");

        foreach (var header in request.Headers)
        {
            Console.WriteLine($"{header.Key}: {header.Value}");
        }

        if (request.Body.CanRead &&
            request.Method is not "GET" and not "HEAD")
        {
            request.EnableBuffering();

            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: true,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();

            request.Body.Position = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                Console.WriteLine("--- Body ---");
                Console.WriteLine(body);
            }
        }

        Console.WriteLine(new string('-', 60));

        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.ContentType = "text/plain; charset=utf-8";

        await context.Response.WriteAsync("OK");
    });

app.Run();