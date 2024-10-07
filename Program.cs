using Microsoft.OpenApi.Models;
using System.Xml.Linq;
namespace Bridges
{
    class Program
    {
        private static XDocument xProtocols = XDocument.Load(@"Data\bridges.xml");
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "Supports chains on protocols",
                    Contact = new OpenApiContact
                    {
                        Name = "Contact on github",
                        Url = new Uri("https://example.com/contact")
                    },
                    License = new OpenApiLicense
                    {
                        Name = "License",
                        Url = new Uri("https://example.com/license")
                    }
                });
            });
            var app = builder.Build();

            if(app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
                    options.RoutePrefix = "api/v1";
                });
            }

            app.UseHttpsRedirection();
            app.MapGet("/", () => Results.Redirect("api/v1")).ExcludeFromDescription();

            app.MapGet("/protocols", () =>
            {
                var allData = xProtocols.Element("bridges")?
                .Elements("protocol")?
                .Select(p => new Protocol
                (p.Attribute("name")?.Value, p.Elements("chain")?.Select(c => c.Value)));

                if(allData is not null)
                {
                    return Results.Ok(allData);
                }
                else
                {
                    return Results.BadRequest();
                }
            })
            .Produces<IEnumerable<Protocol>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("GetAllProtocols")
            .WithTags("Getters");

            app.MapGet("/protocols/{name:alpha}", (string name) =>
            {
                var foundProtocol = xProtocols.Element("bridges")?.Elements("protocol")?.FirstOrDefault(p => p.Attribute("name")?.Value == name.ToLower());
                if (foundProtocol is not null)
                {
                    return Results.Ok(new Protocol(foundProtocol.Attribute("name")?.Value, foundProtocol.Elements("chain").Select(c => c.Value)));
                }
                else
                {
                    return Results.NotFound();
                }
            })
            .Produces<Protocol>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("GetOneProtocolData")
            .WithTags("Getters");

            app.MapPost("/protocols", (string name, string[] chains) =>
            {
                if(!FindByTitle(name, out XElement? element, ref xProtocols))
                {
                    element?.Add(new XElement("protocol",
                        new XAttribute("name", name.ToLower()),
                        chains.Select(c => new XElement("chain", c))));

                    xProtocols.Save(@"Data\bridges.xml");

                    return Results.Created("/protocols", new Protocol(name, chains));
                }
                else
                {
                    return Results.BadRequest();
                }
            })
            .Produces<Protocol>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("CreatNewProtocol")
            .WithTags("Setters");

            app.MapPut("/protocols/{name:alpha}", (string name, string[] args) =>
            {
                if (FindByTitle(name, out XElement? element, ref xProtocols))
                {
                    var foundElement = element?.Elements("protocol").FirstOrDefault(p => p.Attribute("name")?.Value == name.ToLower());
                    var newName = foundElement.Attribute("name").Value;
                    newName = name.ToLower();

                    var chains = foundElement?.Elements("chain");
                    chains = args.Select(c => new XElement("chain", c));

                    xProtocols.Save(@"Data\bridges.xml");
                    return Results.Accepted($"/protocols/{name}", new Protocol(name.ToLower(), args));
                }
                else
                {
                    return Results.NotFound();
                }
            })
            .Produces<Protocol>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("UpdateExistProtocol")
            .WithTags("Setters");

            app.MapDelete("/protocols", (string name) =>
            {
                if (FindByTitle(name, out XElement? elementToRemove, ref xProtocols))
                {
                    elementToRemove?.Elements("protocol").FirstOrDefault(p => p.Attribute("name")?.Value == name.ToLower()).Remove();
                    xProtocols.Save(@"Data\bridges.xml");

                    return Results.Ok();
                }
                else
                {
                    return Results.NotFound();
                }
            })
             .Produces(StatusCodes.Status200OK)
             .Produces(StatusCodes.Status404NotFound)
             .WithName("DeleteOneProtocol")
             .WithTags("Delete");

            app.Run();
        }
        private static bool FindByTitle(string name, out XElement? element, ref XDocument xDoc)
        {
            element = xDoc.Element("bridges");

            if (element?.Elements("protocol").FirstOrDefault(p => p.Attribute("name")?.Value == name.ToLower()) is not null)
                return true;
            else
                return false;
        }
    }
    class Protocol
    {
        public string? Name { get; }
        public IEnumerable<string>? Chains { get; }
        public Protocol(string? name, IEnumerable<string>? chains)
        {
            Name = name;
            Chains = chains;
        }
    }
}