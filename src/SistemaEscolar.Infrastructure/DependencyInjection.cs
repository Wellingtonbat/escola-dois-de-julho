using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;
using SistemaEscolar.Application.Usuarios;
using SistemaEscolar.Infrastructure.Identity;
using SistemaEscolar.Infrastructure.Persistence;
using SistemaEscolar.Infrastructure.Persistence.Interceptors;
using SistemaEscolar.Infrastructure.Persistence.Repositories;
using SistemaEscolar.Infrastructure.Services;

namespace SistemaEscolar.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();
        services.AddScoped<ApplicationDbInitializer>();
        services.AddScoped<IAlunoRepository, AlunoRepository>();
        services.AddScoped<IDisciplinaRepository, DisciplinaRepository>();
        services.AddScoped<IDisciplinaSerieRepository, DisciplinaSerieRepository>();
        services.AddScoped<INotaRepository, NotaRepository>();
        services.AddScoped<IPeriodoRepository, PeriodoRepository>();
        services.AddScoped<IProfessorRepository, ProfessorRepository>();
        services.AddScoped<IProfessorAtribuicaoRepository, ProfessorAtribuicaoRepository>();
        services.AddScoped<IResultadoAcademicoRepository, ResultadoAcademicoRepository>();
        services.AddScoped<ISerieRepository, SerieRepository>();
        services.AddScoped<ITurmaRepository, TurmaRepository>();
        services.AddScoped<IAlunoImportService, AlunoImportService>();
        services.AddScoped<IAccountProvisioner, AccountProvisioner>();
        services.AddScoped<IUsuarioService, UsuarioService>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseNpgsql(connectionString);
            options.AddInterceptors(serviceProvider.GetRequiredService<AuditableEntitySaveChangesInterceptor>());
        });

        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Login";
            options.AccessDeniedPath = "/AccessDenied";
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("DiretorOnly", policy => policy.RequireRole("Diretor"));
            options.AddPolicy("DiretorOrCoordenador", policy => policy.RequireRole("Diretor", "Coordenador", "Cordenador", "Secretaria"));
            options.AddPolicy("ProfessorOrDiretor", policy => policy.RequireRole("Professor", "Diretor"));
        });

        return services;
    }
}
