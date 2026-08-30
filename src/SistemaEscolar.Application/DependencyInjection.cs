using Microsoft.Extensions.DependencyInjection;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Notas;
using SistemaEscolar.Application.Periodos;
using SistemaEscolar.Application.Professores;
using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;

namespace SistemaEscolar.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAlunoService, AlunoService>();
        services.AddScoped<IDisciplinaService, DisciplinaService>();
        services.AddScoped<INotaService, NotaService>();
        services.AddScoped<IPeriodoService, PeriodoService>();
        services.AddScoped<IProfessorService, ProfessorService>();
        services.AddScoped<IResultadoAcademicoService, ResultadoAcademicoService>();
        services.AddScoped<ISerieService, SerieService>();
        services.AddScoped<ITurmaService, TurmaService>();

        return services;
    }
}
