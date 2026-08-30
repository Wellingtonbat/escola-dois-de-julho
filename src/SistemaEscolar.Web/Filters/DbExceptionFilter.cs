using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace SistemaEscolar.Web.Filters;

public sealed class DbExceptionFilter : IAsyncExceptionFilter
{
    private const string UniqueViolationSqlState = "23505";
    private const string ForeignKeyViolationSqlState = "23503";

    private readonly ILogger<DbExceptionFilter> _logger;

    public DbExceptionFilter(ILogger<DbExceptionFilter> logger)
    {
        _logger = logger;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        if (context.Exception is not DbUpdateException dbUpdateException)
        {
            return Task.CompletedTask;
        }

        _logger.LogError(dbUpdateException, "Falha ao salvar alterações no banco de dados em {Path}", context.HttpContext.Request.Path);

        var friendlyMessage = dbUpdateException.InnerException is PostgresException postgresException
            ? postgresException.SqlState switch
            {
                UniqueViolationSqlState => "Já existe um registro com os mesmos dados informados.",
                ForeignKeyViolationSqlState => "Não é possível concluir a operação porque o registro está vinculado a outros dados.",
                _ => "Não foi possível salvar as alterações. Tente novamente."
            }
            : "Não foi possível salvar as alterações. Tente novamente.";

        var tempDataFactory = context.HttpContext.RequestServices.GetRequiredService<ITempDataDictionaryFactory>();
        var tempData = tempDataFactory.GetTempData(context.HttpContext);
        tempData["ErrorMessage"] = friendlyMessage;

        context.Result = new RedirectResult(context.HttpContext.Request.Path + context.HttpContext.Request.QueryString);
        context.ExceptionHandled = true;

        return Task.CompletedTask;
    }
}
