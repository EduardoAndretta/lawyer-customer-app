using LawyerCustomerApp.Application.Configuration;
using LawyerCustomerApp.Domain.Configuration;
using LawyerCustomerApp.External.Configuration;
using LawyerCustomerApp.External.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.UseOneOfForPolymorphism();

    options.CustomSchemaIds(x => x.FullName);
});

builder.Services.AddControllers();
builder.Services.AddSignalR();

builder.Services.AddLocalizationConfiguration();

builder.Services.AddIdentityConfiguration(builder.Configuration);

// [Documentation]
builder.Services.AddSwaggerConfiguration();

// [Dependencies]
builder.Services.AddDomainDependenciesAndConfiguration();
builder.Services.AddExternalDependenciesAndConfiguration();

try
{
    var app = builder.Build();

    app.UseExceptionConfiguration();

    app.UseLocalizationConfiguration();

    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // [Documentation]
    app.UseSwaggerConfiguration();
    app.UseReDocConfiguration();

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
        //endpoints.MapHub<ChatHub>("/chat");
    });

    app.UseHttpsRedirection();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    throw ex;
}

