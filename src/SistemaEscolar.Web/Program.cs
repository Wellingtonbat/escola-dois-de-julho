using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using SistemaEscolar.Application;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Infrastructure;
using SistemaEscolar.Infrastructure.Identity;
using SistemaEscolar.Infrastructure.Persistence;
using SistemaEscolar.Web.Filters;
using SistemaEscolar.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services
    .AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AllowAnonymousToPage("/Login");
        options.Conventions.AllowAnonymousToPage("/AccessDenied");
        options.Conventions.AllowAnonymousToPage("/Error");
    })
    .AddMvcOptions(options => options.Filters.Add<DbExceptionFilter>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbInitializer>();
    await initialiser.InitialiseAsync();
    await initialiser.SeedAsync(seedSampleData: app.Environment.IsDevelopment());
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Atrás de um proxy reverso (Render, etc.) que termina o TLS, o app enxerga a requisição
// como HTTP. Sem isto, UseHttpsRedirection() causaria um loop infinito de redirecionamento.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
// O IP do proxy do Render não é conhecido de antemão, então limpamos as listas
// padrão (que só confiam na rede local) para aceitar os cabeçalhos encaminhados por ele.
forwardedHeadersOptions.KnownIPNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var isPaginaIsenta = path.StartsWithSegments("/TrocarSenha")
        || path.StartsWithSegments("/Logout")
        || System.IO.Path.HasExtension(path.Value);

    if (context.User.Identity?.IsAuthenticated == true && !isPaginaIsenta)
    {
        var userManager = context.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.User);
        if (user is { MustChangePassword: true })
        {
            context.Response.Redirect("/TrocarSenha");
            return;
        }
    }

    await next();
});

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
