using System.Globalization;
using System.Text.Json;
using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Auditoria;

public sealed class AuditoriaService : IAuditoriaService
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    // Horário de Brasília (UTC-3, sem horário de verão desde 2019). Evita depender do nome do fuso do servidor.
    private static readonly TimeSpan OffsetBrasilia = TimeSpan.FromHours(-3);

    private const int TamanhoPaginaMaximo = 100;
    private const int MaxCamposPorRegistro = 14;

    private static readonly IReadOnlyList<AuditoriaEntidadeDto> EntidadesConhecidas = new[]
    {
        new AuditoriaEntidadeDto("Notas", "Notas"),
        new AuditoriaEntidadeDto("RecuperacoesFinais", "Recuperação final"),
        new AuditoriaEntidadeDto("Alunos", "Alunos"),
        new AuditoriaEntidadeDto("Professores", "Professores"),
        new AuditoriaEntidadeDto("ProfessorAtribuicoes", "Vínculos de professor"),
        new AuditoriaEntidadeDto("Turmas", "Turmas"),
        new AuditoriaEntidadeDto("Series", "Séries"),
        new AuditoriaEntidadeDto("Disciplinas", "Disciplinas"),
        new AuditoriaEntidadeDto("DisciplinaSeries", "Disciplinas por série"),
        new AuditoriaEntidadeDto("PeriodosLancamento", "Períodos de lançamento"),
        new AuditoriaEntidadeDto("AspNetUsers", "Usuários (contas)"),
        new AuditoriaEntidadeDto("AspNetUserRoles", "Perfis de usuário"),
    };

    private static readonly IReadOnlyDictionary<string, string> RotulosDeCampos = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Nome"] = "Nome",
        ["NomeCompleto"] = "Nome completo",
        ["FullName"] = "Nome completo",
        ["Email"] = "E-mail",
        ["UserName"] = "Login (CPF)",
        ["UsuarioCpf"] = "CPF do usuário",
        ["Cpf"] = "CPF",
        ["Matricula"] = "Matrícula",
        ["DataNascimento"] = "Data de nascimento",
        ["AnoLetivo"] = "Ano letivo",
        ["Bimestre"] = "Trimestre",
        ["Trimestre"] = "Trimestre",
        ["Descricao"] = "Descrição",
        ["DataInicial"] = "Data inicial",
        ["DataFinal"] = "Data final",
        ["IsAberto"] = "Aberto",
        ["AbertoManualmente"] = "Aberto manualmente",
        ["Avaliacao1"] = "1ª avaliação",
        ["Avaliacao2"] = "2ª avaliação",
        ["Avaliacao3"] = "3ª avaliação",
        ["RecuperacaoParalela"] = "Recuperação paralela",
        ["ResultadoUnidade"] = "Resultado da unidade",
        ["ResultadoFinalUnidade"] = "Resultado final da unidade",
        ["Valor"] = "Valor",
        ["IsFinalizada"] = "Finalizada",
        ["IsAtivo"] = "Ativo",
        ["IsAtiva"] = "Ativa",
        ["IsActive"] = "Conta ativa",
        ["IsDeleted"] = "Excluído",
        ["MustChangePassword"] = "Troca de senha obrigatória",
        ["PasswordHash"] = "Senha",
        ["Turno"] = "Turno",
        ["Ordem"] = "Ordem",
        ["Codigo"] = "Código",
        ["CargaHoraria"] = "Carga horária",
        ["AlunoId"] = "Aluno",
        ["DisciplinaId"] = "Disciplina",
        ["ProfessorId"] = "Professor",
        ["PeriodoLancamentoId"] = "Período",
        ["TurmaId"] = "Turma",
        ["SerieId"] = "Série",
        ["UserId"] = "Usuário",
        ["RoleId"] = "Perfil",
    };

    // Campos que não interessam a quem lê a auditoria (técnicos ou derivados de outros campos).
    private static readonly HashSet<string> CamposOcultos = new(StringComparer.Ordinal)
    {
        "CreatedAtUtc", "CreatedBy", "UpdatedAtUtc", "UpdatedBy", "NormalizedUserName", "NormalizedEmail",
        // Controle interno do Identity (contadores de login, carimbos de segurança, confirmações padrão).
        "ConcurrencyStamp", "SecurityStamp", "AccessFailedCount", "LockoutEnd", "LockoutEnabled",
        "EmailConfirmed", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled"
    };

    // Campos que apontam para outro registro (o valor é um Id que pode ser trocado pelo nome).
    private static readonly HashSet<string> CamposDeReferencia = new(StringComparer.Ordinal)
    {
        "AlunoId", "DisciplinaId", "ProfessorId", "PeriodoLancamentoId", "TurmaId", "SerieId", "UserId", "RoleId"
    };

    private readonly IAuditoriaRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public AuditoriaService(IAuditoriaRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    public IReadOnlyList<AuditoriaEntidadeDto> Entidades => EntidadesConhecidas;

    public async Task<AuditoriaListResult> ListarAsync(AuditoriaFiltro filtro, CancellationToken cancellationToken = default)
    {
        // A auditoria expõe dados de todos os usuários: só a Diretoria acessa, também aqui no servidor.
        if (!PermissoesPerfil.PodeVerAuditoria(_currentUserService.IsInRole))
        {
            return AuditoriaListResult.Fail("Somente Diretor ou Vice-Diretor podem consultar a auditoria.");
        }

        var tamanho = Math.Clamp(filtro.TamanhoPagina, 1, TamanhoPaginaMaximo);
        var pagina = Math.Max(1, filtro.Pagina);

        var tabela = EntidadesConhecidas.Any(x => x.Tabela == filtro.Entidade) ? filtro.Entidade : null;
        var acao = filtro.Acao is "criado" or "alterado" or "excluido" ? filtro.Acao : null;

        // As datas do filtro são dias de Brasília; no banco tudo está em UTC.
        DateTime? deUtc = filtro.DataInicial.HasValue ? filtro.DataInicial.Value.Date - OffsetBrasilia : null;
        DateTime? ateUtc = filtro.DataFinal.HasValue ? filtro.DataFinal.Value.Date.AddDays(1) - OffsetBrasilia : null;

        var (registros, total) = await _repository.ListarAsync(
            deUtc, ateUtc, filtro.Usuario, tabela, acao, (pagina - 1) * tamanho, tamanho, cancellationToken);

        var totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamanho));

        var analisados = registros.Select(Analisar).ToList();
        var nomes = await _repository.ObterNomesReferenciasAsync(ColetarIds(analisados, registros), cancellationToken);

        var itens = analisados
            .Select(x => MontarItem(x, nomes))
            .ToList();

        return AuditoriaListResult.Success(new AuditoriaPaginaDto(itens, pagina, totalPaginas, total));
    }

    // ----- Leitura dos registros -----

    private sealed record Analisado(
        AuditLog Registro,
        Dictionary<string, JsonElement> Chave,
        Dictionary<string, JsonElement> Antes,
        Dictionary<string, JsonElement> Depois);

    private static Analisado Analisar(AuditLog registro) =>
        new(registro, LerJson(registro.KeyValues), LerJson(registro.OldValues), LerJson(registro.NewValues));

    private static Dictionary<string, JsonElement> LerJson(string? json)
    {
        var resultado = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(json))
        {
            return resultado;
        }

        try
        {
            using var documento = JsonDocument.Parse(json);
            if (documento.RootElement.ValueKind != JsonValueKind.Object)
            {
                return resultado;
            }

            foreach (var propriedade in documento.RootElement.EnumerateObject())
            {
                resultado[propriedade.Name] = propriedade.Value.Clone();
            }
        }
        catch (JsonException)
        {
            // Registro antigo ou corrompido: mostra o que der, sem quebrar a tela.
        }

        return resultado;
    }

    private static IReadOnlyCollection<Guid> ColetarIds(IReadOnlyList<Analisado> analisados, IReadOnlyList<AuditLog> registros)
    {
        var ids = new HashSet<Guid>();

        foreach (var registro in registros)
        {
            if (Guid.TryParse(registro.CreatedBy, out var usuarioId))
            {
                ids.Add(usuarioId);
            }
        }

        foreach (var analisado in analisados)
        {
            foreach (var origem in new[] { analisado.Chave, analisado.Antes, analisado.Depois })
            {
                foreach (var (campo, valor) in origem)
                {
                    if (CamposDeReferencia.Contains(campo)
                        && valor.ValueKind == JsonValueKind.String
                        && Guid.TryParse(valor.GetString(), out var id))
                    {
                        ids.Add(id);
                    }
                }
            }
        }

        return ids;
    }

    // ----- Montagem dos itens exibidos -----

    private static AuditoriaItemDto MontarItem(Analisado analisado, IReadOnlyDictionary<Guid, string> nomes)
    {
        var registro = analisado.Registro;
        var (acao, classe) = ClassificarAcao(analisado);

        return new AuditoriaItemDto(
            registro.Id,
            registro.CreatedAtUtc + OffsetBrasilia,
            DescreverUsuario(registro, nomes),
            registro.UserName,
            DescreverEntidade(registro.TableName),
            acao,
            classe,
            DescreverRegistro(analisado, nomes),
            MontarAlteracoes(analisado, nomes));
    }

    private static string DescreverUsuario(AuditLog registro, IReadOnlyDictionary<Guid, string> nomes)
    {
        if (!string.IsNullOrWhiteSpace(registro.UserFullName))
        {
            return registro.UserFullName;
        }

        if (Guid.TryParse(registro.CreatedBy, out var usuarioId) && nomes.TryGetValue(usuarioId, out var nome))
        {
            return nome;
        }

        if (!string.IsNullOrWhiteSpace(registro.UserName))
        {
            return $"CPF {registro.UserName}";
        }

        return string.IsNullOrWhiteSpace(registro.CreatedBy)
            ? "Sistema / não identificado"
            : "Usuário removido";
    }

    private static string DescreverEntidade(string tabela) =>
        EntidadesConhecidas.FirstOrDefault(x => x.Tabela == tabela)?.Rotulo ?? tabela;

    private static (string Rotulo, string Classe) ClassificarAcao(Analisado analisado)
    {
        switch (analisado.Registro.Action)
        {
            case "ADDED":
                return ("Criado", "status-open");
            case "DELETED":
                return ("Excluído", "status-danger");
        }

        var excluidoAntes = LerBool(analisado.Antes, "IsDeleted");
        var excluidoDepois = LerBool(analisado.Depois, "IsDeleted");

        if (excluidoAntes == false && excluidoDepois == true)
        {
            return ("Excluído", "status-danger");
        }

        if (excluidoAntes == true && excluidoDepois == false)
        {
            return ("Restaurado", "status-neutral");
        }

        return ("Alterado", "status-warn");
    }

    private static bool? LerBool(Dictionary<string, JsonElement> valores, string campo)
    {
        if (!valores.TryGetValue(campo, out var valor))
        {
            return null;
        }

        return valor.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static string? LerTexto(Analisado analisado, string campo)
    {
        foreach (var origem in new[] { analisado.Depois, analisado.Antes, analisado.Chave })
        {
            if (origem.TryGetValue(campo, out var valor) && valor.ValueKind == JsonValueKind.String)
            {
                var texto = valor.GetString();
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    return texto;
                }
            }
        }

        return null;
    }

    private static string? LerReferencia(Analisado analisado, string campo, IReadOnlyDictionary<Guid, string> nomes)
    {
        var texto = LerTexto(analisado, campo);
        if (texto is null)
        {
            return null;
        }

        if (Guid.TryParse(texto, out var id) && nomes.TryGetValue(id, out var nome))
        {
            return campo == "RoleId" ? Perfis.Descrever(nome) : nome;
        }

        return Encurtar(texto);
    }

    private static string DescreverRegistro(Analisado analisado, IReadOnlyDictionary<Guid, string> nomes)
    {
        string Junta(params string?[] partes) =>
            string.Join(" • ", partes.Where(x => !string.IsNullOrWhiteSpace(x)));

        var descricao = analisado.Registro.TableName switch
        {
            "Alunos" or "Professores" => LerTexto(analisado, "NomeCompleto"),
            "Disciplinas" or "Turmas" or "Series" => LerTexto(analisado, "Nome"),
            "PeriodosLancamento" => LerTexto(analisado, "Descricao"),
            "Notas" => Junta(
                LerReferencia(analisado, "AlunoId", nomes),
                LerReferencia(analisado, "DisciplinaId", nomes),
                LerReferencia(analisado, "PeriodoLancamentoId", nomes)),
            "RecuperacoesFinais" => Junta(
                LerReferencia(analisado, "AlunoId", nomes),
                LerReferencia(analisado, "DisciplinaId", nomes),
                LerTexto(analisado, "AnoLetivo") ?? LerNumero(analisado, "AnoLetivo")),
            "ProfessorAtribuicoes" => Junta(
                LerReferencia(analisado, "ProfessorId", nomes),
                LerReferencia(analisado, "TurmaId", nomes),
                LerReferencia(analisado, "DisciplinaId", nomes)),
            "DisciplinaSeries" => Junta(
                LerReferencia(analisado, "DisciplinaId", nomes),
                LerReferencia(analisado, "SerieId", nomes)),
            "AspNetUsers" => LerTexto(analisado, "FullName") ?? LerTexto(analisado, "UserName"),
            "AspNetUserRoles" => Junta(
                LerReferencia(analisado, "UserId", nomes),
                LerReferencia(analisado, "RoleId", nomes)),
            _ => null
        };

        return string.IsNullOrWhiteSpace(descricao) ? "—" : descricao;
    }

    private static string? LerNumero(Analisado analisado, string campo)
    {
        foreach (var origem in new[] { analisado.Depois, analisado.Antes })
        {
            if (origem.TryGetValue(campo, out var valor) && valor.ValueKind == JsonValueKind.Number)
            {
                return valor.GetRawText();
            }
        }

        return null;
    }

    private static IReadOnlyList<AuditoriaAlteracaoDto> MontarAlteracoes(Analisado analisado, IReadOnlyDictionary<Guid, string> nomes)
    {
        var alteracoes = new List<AuditoriaAlteracaoDto>();

        switch (analisado.Registro.Action)
        {
            case "ADDED":
                foreach (var (campo, valor) in analisado.Depois)
                {
                    if (CampoIrrelevante(campo) || valor.ValueKind == JsonValueKind.Null || EhFalsoPadrao(campo, valor))
                    {
                        continue;
                    }

                    alteracoes.Add(new AuditoriaAlteracaoDto(RotularCampo(campo), null, Formatar(campo, valor, nomes)));
                }

                break;

            case "DELETED":
                foreach (var (campo, valor) in analisado.Antes)
                {
                    if (CampoIrrelevante(campo) || valor.ValueKind == JsonValueKind.Null || EhFalsoPadrao(campo, valor))
                    {
                        continue;
                    }

                    alteracoes.Add(new AuditoriaAlteracaoDto(RotularCampo(campo), Formatar(campo, valor, nomes), null));
                }

                break;

            default:
                foreach (var (campo, novo) in analisado.Depois)
                {
                    if (CampoIrrelevante(campo))
                    {
                        continue;
                    }

                    analisado.Antes.TryGetValue(campo, out var anterior);
                    if (JsonSemanticamenteIgual(anterior, novo))
                    {
                        continue;
                    }

                    alteracoes.Add(new AuditoriaAlteracaoDto(
                        RotularCampo(campo),
                        anterior.ValueKind == JsonValueKind.Undefined ? "—" : Formatar(campo, anterior, nomes),
                        Formatar(campo, novo, nomes)));
                }

                break;
        }

        return alteracoes.Take(MaxCamposPorRegistro).ToList();
    }

    private static bool CampoIrrelevante(string campo) => CamposOcultos.Contains(campo);

    // Em criações, "Excluído: Não" é o padrão e só polui a lista.
    private static bool EhFalsoPadrao(string campo, JsonElement valor) =>
        campo == "IsDeleted" && valor.ValueKind == JsonValueKind.False;

    private static bool JsonSemanticamenteIgual(JsonElement a, JsonElement b)
    {
        if (a.ValueKind == JsonValueKind.Undefined)
        {
            return false;
        }

        return a.ValueKind == b.ValueKind && a.GetRawText() == b.GetRawText();
    }

    private static string RotularCampo(string campo) =>
        RotulosDeCampos.TryGetValue(campo, out var rotulo) ? rotulo : campo;

    private static string Formatar(string campo, JsonElement valor, IReadOnlyDictionary<Guid, string> nomes)
    {
        switch (valor.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return "—";
            case JsonValueKind.True:
                return "Sim";
            case JsonValueKind.False:
                return "Não";
            case JsonValueKind.Number:
                return valor.TryGetDecimal(out var numero)
                    ? numero.ToString("0.##", PtBr)
                    : valor.GetRawText();
            case JsonValueKind.String:
                var texto = valor.GetString() ?? string.Empty;

                // Defesa em profundidade: senha nunca é exibida, mesmo que um registro antigo ainda a contenha.
                if (campo == "PasswordHash")
                {
                    return texto == "[alterado]" ? texto : "[oculto]";
                }

                if (Guid.TryParse(texto, out var id))
                {
                    if (nomes.TryGetValue(id, out var nome))
                    {
                        return campo == "RoleId" ? Perfis.Descrever(nome) : nome;
                    }

                    return Encurtar(texto);
                }

                if (texto.Length >= 10 && char.IsDigit(texto[0]) && DateTime.TryParse(texto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var data))
                {
                    // Datas gravadas em UTC (terminam em "Z") são mostradas no horário de Brasília.
                    if (data.Kind == DateTimeKind.Utc)
                    {
                        data += OffsetBrasilia;
                    }

                    return texto.Length > 10 ? data.ToString("dd/MM/yyyy HH:mm", PtBr) : data.ToString("dd/MM/yyyy", PtBr);
                }

                return texto.Length > 140 ? texto[..140] + "…" : (texto.Length == 0 ? "—" : texto);
            default:
                return valor.GetRawText();
        }
    }

    private static string Encurtar(string texto) =>
        texto.Length > 8 ? texto[..8] + "…" : texto;
}
