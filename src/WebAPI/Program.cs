using DotNetEnv;
using Infrastructure;
using Infrastructure.Constants;
using Infrastructure.Security.Permission;
using Infrastructure.Security;
using Core.Application.Interface.Security;
using Infrastructure.Services.Security;
using Core.Application.Interface.Repository.SEIH;
using Infrastructure.Repository.SEIH.Hospital;
using Microsoft.AspNetCore.Identity;
using Core.Application.Interface.Services.SEIH.Transfer;
using Infrastructure.Services.SEIH.Transfer;
using Infrastructure.Repository.SEIH.Transfer;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Services.AddControllers();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<IInstitutionKeyService, InstitutionKeyService>();
builder.Services.AddScoped<IInstitutionKeyRepository, InstitutionKeyRepository>();
builder.Services.AddScoped<ITransferRequestNetworkService, TransferRequestNetworkService>();
builder.Services.AddScoped<ITransferRequestRepository, TransferRequestRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    options.TokenLifespan = TimeSpan.FromHours(3));

builder.Logging.AddConsole();

var app = builder.Build();

app.UseCors("GeneralPolicy");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseForwardedHeaders();
}

app.UseHsts();
app.UseHttpsRedirection();
app.UseForwardedHeaders();
app.UseCookiePolicy();
app.UseSession();


// ========================================================
// 🔐 REST SECURITY PIPELINE (DME / Système externe)
// S’applique uniquement aux routes /api/rest/*
// ========================================================

app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/api/rest"),
    restApp =>
    {
        Console.WriteLine("REST PIPELINE TRIGGERED");

        restApp.UseMiddleware<IpWhitelistMiddleware>();
        restApp.UseMiddleware<ApiKeyMiddleware>();
        restApp.UseWhen(
    context =>
        !context.Request.Path.StartsWithSegments("/api/rest/seih/hospital/network"),
    inner =>
    {
        inner.UseMiddleware<SignatureMiddleware>();
    });
    }
);


// ========================================================
// 👤 AUTHENTIFICATION UTILISATEUR (TransfertDirect)
// ========================================================

// app.UseAuthentication();
// app.UseAuthorization();


// ========================================================
// 🔐 Permissions métier (après auth si JWT présent)
// ========================================================

// app.UseMiddleware<PermissionMiddleware>();


app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api/rest"),
    uiApp =>
    {
        uiApp.UseAuthentication();
        uiApp.UseAuthorization();
        // uiApp.UseMiddleware<PermissionMiddleware>();
    }
);


// ========================================================
// 🎯 Endpoints
// ========================================================

app.MapControllers();

app.Run();
