using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Application.Turmas;
using SistemaEscolar.Domain.Entities;

namespace SistemaEscolar.Application.Atas;

public sealed class AtaService : IAtaService
{
    private const string MensagemSomenteDiretoria = "Somente Diretor ou Vice-Diretor podem editar uma Ata já finalizada.";
    private const string MensagemSemPermissao = "Você não tem permissão para gerenciar Atas.";

    private readonly IAtaRepository _ataRepository;
    private readonly ITurmaService _turmaService;
    private readonly IAlunoService _alunoService;
    private readonly IResultadoAcademicoService _resultadoAcademicoService;
    private readonly IDisciplinaService _disciplinaService;
    private readonly ICurrentUserService _currentUserService;

    public AtaService(
        IAtaRepository ataRepository,
        ITurmaService turmaService,
        IAlunoService alunoService,
        IResultadoAcademicoService resultadoAcademicoService,
        IDisciplinaService disciplinaService,
        ICurrentUserService currentUserService)
    {
        _ataRepository = ataRepository;
        _turmaService = turmaService;
        _alunoService = alunoService;
        _resultadoAcademicoService = resultadoAcademicoService;
        _disciplinaService = disciplinaService;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<AtaListItemDto>> ListarAsync(int? anoLetivo = null, CancellationToken cancellationToken = default)
    {
        var atas = await _ataRepository.GetAllAsync(cancellationToken);
        var turmas = (await _turmaService.ListarAsync(null, cancellationToken)).ToDictionary(x => x.Id);

        var resultado = new List<AtaListItemDto>();
        foreach (var ata in atas)
        {
            if (!turmas.TryGetValue(ata.TurmaId, out var turma))
            {
                continue;
            }

            if (anoLetivo.HasValue && turma.AnoLetivo != anoLetivo.Value)
            {
                continue;
            }

            var itens = await _ataRepository.GetItensAsync(ata.Id, cancellationToken);

            resultado.Add(new AtaListItemDto(
                ata.Id,
                ata.TurmaId,
                turma.Nome,
                turma.SerieNome,
                turma.AnoLetivo,
                ata.Status,
                itens.Count,
                itens.Count(x => x.RF != AtaConstantes.RFPendente),
                ata.FinalizadaEmUtc,
                ata.FinalizadaPorNome));
        }

        return resultado
            .OrderByDescending(x => x.AnoLetivo)
            .ThenBy(x => x.TurmaNome)
            .ToList();
    }

    public async Task<IReadOnlyList<AtaTurmaDisponivelDto>> ListarTurmasSemAtaAsync(int anoLetivo, CancellationToken cancellationToken = default)
    {
        var turmas = await _turmaService.ListarAsync(new TurmaListFilter(null, null, anoLetivo, null), cancellationToken);
        var atas = await _ataRepository.GetAllAsync(cancellationToken);
        var turmasComAta = atas.Select(x => x.TurmaId).ToHashSet();

        return turmas
            .Where(x => !turmasComAta.Contains(x.Id))
            .OrderBy(x => x.SerieNome)
            .ThenBy(x => x.Nome)
            .Select(x => new AtaTurmaDisponivelDto(x.Id, x.Nome, x.SerieNome, x.AnoLetivo))
            .ToList();
    }

    public async Task<(AtaResult Result, Guid? AtaId)> CriarParaTurmaAsync(Guid turmaId, CancellationToken cancellationToken = default)
    {
        if (!PermissoesPerfil.PodeAcessarAtas(_currentUserService.IsInRole))
        {
            return (AtaResult.Fail(MensagemSemPermissao), null);
        }

        var turma = await _turmaService.ObterPorIdAsync(turmaId, cancellationToken);
        if (turma is null)
        {
            return (AtaResult.Fail("Turma não encontrada."), null);
        }

        if (await _ataRepository.GetByTurmaIdAsync(turmaId, cancellationToken) is not null)
        {
            return (AtaResult.Fail("Esta turma já tem uma Ata cadastrada."), null);
        }

        var alunos = (await _alunoService.ListarAsync(new AlunoListFilter(null, null, turmaId, true), cancellationToken))
            .OrderBy(x => x.NomeCompleto)
            .ToList();

        var resultados = await _resultadoAcademicoService.ListarAsync(
            new ResultadoAcademicoFilter(turma.AnoLetivo, turma.Nome, null, "todos"), cancellationToken);
        var resultadosPorAluno = resultados.ToLookup(x => x.AlunoId);

        var codigoPorDisciplina = (await _disciplinaService.ListarAsync(null, cancellationToken))
            .ToDictionary(x => x.Id, x => x.Codigo);

        var ata = new Ata
        {
            Id = Guid.NewGuid(),
            TurmaId = turmaId,
            Status = AtaConstantes.StatusRascunho
        };

        var itens = new List<(AtaAluno Item, IReadOnlyList<AtaAlunoDisciplina> Disciplinas)>();
        foreach (var aluno in alunos)
        {
            var resultadosDoAluno = resultadosPorAluno[aluno.Id].ToList();

            var disciplinas = resultadosDoAluno
                .Select(r => new AtaAlunoDisciplina
                {
                    Id = Guid.NewGuid(),
                    DisciplinaId = r.DisciplinaId,
                    DisciplinaNome = r.Disciplina,
                    DisciplinaCodigo = codigoPorDisciplina.GetValueOrDefault(r.DisciplinaId, string.Empty),
                    ResultadoFinalAno = r.ResultadoFinalAno
                })
                .ToList();

            // Só fica "Aprovado" automático quando toda disciplina já está com o ano completo e média
            // final ≥ 5 (é exatamente isso que Situacao == "Aprovado" já garante em Resultados). Qualquer
            // disciplina pendente ou abaixo da média deixa o aluno como "Pendente", liberando o AvF.
            var todasAprovadas = disciplinas.Count > 0 && resultadosDoAluno.All(r => r.Situacao == "Aprovado");

            var item = new AtaAluno
            {
                Id = Guid.NewGuid(),
                AlunoId = aluno.Id,
                TP = Math.Round(disciplinas.Sum(d => d.ResultadoFinalAno), 2),
                MC = disciplinas.Count > 0 ? Math.Round(disciplinas.Average(d => d.ResultadoFinalAno), 2) : 0m,
                AvF = null,
                RF = todasAprovadas ? AtaConstantes.RFAprovado : AtaConstantes.RFPendente
            };

            itens.Add((item, disciplinas));
        }

        await _ataRepository.CriarComItensAsync(ata, itens, cancellationToken);
        return (AtaResult.Success(), ata.Id);
    }

    public async Task<AtaDetalheDto?> ObterDetalheAsync(Guid ataId, CancellationToken cancellationToken = default)
    {
        var ata = await _ataRepository.GetByIdAsync(ataId, cancellationToken);
        if (ata is null)
        {
            return null;
        }

        var turma = await _turmaService.ObterPorIdAsync(ata.TurmaId, cancellationToken);
        if (turma is null)
        {
            return null;
        }

        var alunos = (await _alunoService.ListarAsync(null, cancellationToken)).ToDictionary(x => x.Id);

        var itensEntidade = (await _ataRepository.GetItensAsync(ataId, cancellationToken))
            .OrderBy(x => alunos.TryGetValue(x.AlunoId, out var al) ? al.NomeCompleto : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var disciplinasPorItem = (await _ataRepository.GetDisciplinasAsync(itensEntidade.Select(x => x.Id).ToList(), cancellationToken))
            .ToLookup(x => x.AtaAlunoId);

        var disciplinasColunas = disciplinasPorItem
            .SelectMany(g => g)
            .GroupBy(x => x.DisciplinaId)
            .Select(g => new AtaDisciplinaColunaDto(g.Key, g.First().DisciplinaNome, g.First().DisciplinaCodigo))
            .OrderBy(x => x.Nome, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var itens = new List<AtaAlunoItemDto>();
        var numero = 1;
        foreach (var item in itensEntidade)
        {
            var aluno = alunos.TryGetValue(item.AlunoId, out var a) ? a : null;
            var disciplinasDoItem = disciplinasPorItem[item.Id]
                .OrderBy(x => x.DisciplinaNome, StringComparer.OrdinalIgnoreCase)
                .Select(x => new AtaAlunoDisciplinaDto(x.DisciplinaId, x.DisciplinaNome, x.DisciplinaCodigo, x.ResultadoFinalAno))
                .ToList();

            // O campo AvF só fica desabilitado quando o aluno foi aprovado automaticamente (toda
            // disciplina ≥ 5, sem precisar de AvF) ou quando a situação foi definida manualmente como
            // Transferido. Em qualquer outro caso (Pendente, Conservado, ou Aprovado através do AvF)
            // ele continua editável.
            var avfHabilitado = item.RF != AtaConstantes.RFTransferido
                && !(item.RF == AtaConstantes.RFAprovado && !item.AvF.HasValue);

            itens.Add(new AtaAlunoItemDto(
                item.Id,
                item.AlunoId,
                numero++,
                aluno?.NomeCompleto ?? "Aluno não encontrado",
                aluno?.MatriculaPrefeitura,
                disciplinasDoItem,
                item.TP,
                item.MC,
                item.AvF,
                avfHabilitado,
                item.RF));
        }

        var podeEditar = ata.Status == AtaConstantes.StatusRascunho
            ? PermissoesPerfil.PodeAcessarAtas(_currentUserService.IsInRole)
            : PermissoesPerfil.PodeEditarAtaFinalizada(_currentUserService.IsInRole);

        return new AtaDetalheDto(
            ata.Id,
            ata.TurmaId,
            turma.Nome,
            turma.SerieNome,
            turma.AnoLetivo,
            ata.Status,
            podeEditar,
            ata.FinalizadaEmUtc,
            ata.FinalizadaPorNome,
            disciplinasColunas,
            itens);
    }

    public async Task<AtaResult> SalvarMatriculaAsync(Guid ataAlunoId, string? matriculaPrefeitura, CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarItemEditavelAsync(ataAlunoId, cancellationToken);
        if (!contexto.Result.Succeeded)
        {
            return contexto.Result;
        }

        // A matrícula da prefeitura é a mesma do cadastro de Alunos — editar aqui atualiza lá também.
        return await AtualizarMatriculaDoAlunoAsync(contexto.Item!.AlunoId, matriculaPrefeitura, cancellationToken);
    }

    public async Task<AtaResult> SalvarAvFAsync(Guid ataAlunoId, decimal? valor, CancellationToken cancellationToken = default)
    {
        var contexto = await CarregarItemEditavelAsync(ataAlunoId, cancellationToken);
        if (!contexto.Result.Succeeded)
        {
            return contexto.Result;
        }

        if (valor.HasValue && (valor.Value < 0m || valor.Value > 10m))
        {
            return AtaResult.Fail("A Avaliação Final deve estar entre 0 e 10.");
        }

        var item = contexto.Item!;
        item.AvF = valor.HasValue ? Math.Round(valor.Value, 2) : null;
        item.RF = valor switch
        {
            null => AtaConstantes.RFPendente,
            { } v when v >= AtaConstantes.MediaAprovacao => AtaConstantes.RFAprovado,
            _ => AtaConstantes.RFConservado
        };

        await _ataRepository.AtualizarItemAsync(item, cancellationToken);
        return AtaResult.Success();
    }

    public async Task<AtaResult> DefinirRFManualAsync(Guid ataAlunoId, string rf, CancellationToken cancellationToken = default)
    {
        if (!AtaConstantes.ValoresRF.Contains(rf))
        {
            return AtaResult.Fail("Situação final inválida.");
        }

        var contexto = await CarregarItemEditavelAsync(ataAlunoId, cancellationToken);
        if (!contexto.Result.Succeeded)
        {
            return contexto.Result;
        }

        contexto.Item!.RF = rf;
        await _ataRepository.AtualizarItemAsync(contexto.Item, cancellationToken);
        return AtaResult.Success();
    }

    public async Task<AtaResult> FinalizarAsync(Guid ataId, CancellationToken cancellationToken = default)
    {
        if (!PermissoesPerfil.PodeAcessarAtas(_currentUserService.IsInRole))
        {
            return AtaResult.Fail(MensagemSemPermissao);
        }

        var ata = await _ataRepository.GetByIdAsync(ataId, cancellationToken);
        if (ata is null)
        {
            return AtaResult.Fail("Ata não encontrada.");
        }

        if (ata.Status == AtaConstantes.StatusFinalizada)
        {
            return AtaResult.Fail("Esta Ata já está finalizada.");
        }

        var itens = await _ataRepository.GetItensAsync(ataId, cancellationToken);
        if (itens.Count == 0 || itens.Any(x => x.RF == AtaConstantes.RFPendente))
        {
            return AtaResult.Fail("Ainda há aluno(s) sem resultado final definido.");
        }

        ata.Status = AtaConstantes.StatusFinalizada;
        ata.FinalizadaEmUtc = DateTime.UtcNow;
        ata.FinalizadaPorUserId = _currentUserService.UserId;
        ata.FinalizadaPorNome = _currentUserService.FullName ?? _currentUserService.UserName;

        await _ataRepository.AtualizarAsync(ata, cancellationToken);
        return AtaResult.Success();
    }

    public async Task<AtaResult> HabilitarEdicaoAsync(Guid ataId, CancellationToken cancellationToken = default)
    {
        if (!PermissoesPerfil.PodeEditarAtaFinalizada(_currentUserService.IsInRole))
        {
            return AtaResult.Fail(MensagemSomenteDiretoria);
        }

        var ata = await _ataRepository.GetByIdAsync(ataId, cancellationToken);
        if (ata is null)
        {
            return AtaResult.Fail("Ata não encontrada.");
        }

        ata.Status = AtaConstantes.StatusRascunho;
        ata.FinalizadaEmUtc = null;
        ata.FinalizadaPorUserId = null;
        ata.FinalizadaPorNome = null;

        await _ataRepository.AtualizarAsync(ata, cancellationToken);
        return AtaResult.Success();
    }

    // Carrega um item (linha de aluno) validando que a Ata que o contém pode ser editada por quem está
    // pedindo a alteração agora: rascunho = qualquer um do grupo de gestão; finalizada = só Diretoria.
    private async Task<(AtaResult Result, AtaAluno? Item)> CarregarItemEditavelAsync(Guid ataAlunoId, CancellationToken cancellationToken)
    {
        var item = await _ataRepository.GetItemByIdAsync(ataAlunoId, cancellationToken);
        if (item is null)
        {
            return (AtaResult.Fail("Lançamento não encontrado."), null);
        }

        var ata = await _ataRepository.GetByIdAsync(item.AtaId, cancellationToken);
        if (ata is null)
        {
            return (AtaResult.Fail("Ata não encontrada."), null);
        }

        var podeEditar = ata.Status == AtaConstantes.StatusRascunho
            ? PermissoesPerfil.PodeAcessarAtas(_currentUserService.IsInRole)
            : PermissoesPerfil.PodeEditarAtaFinalizada(_currentUserService.IsInRole);

        if (!podeEditar)
        {
            return (AtaResult.Fail(ata.Status == AtaConstantes.StatusFinalizada ? MensagemSomenteDiretoria : MensagemSemPermissao), null);
        }

        return (AtaResult.Success(), item);
    }

    private async Task<AtaResult> AtualizarMatriculaDoAlunoAsync(Guid alunoId, string? matriculaPrefeitura, CancellationToken cancellationToken)
    {
        var resultado = await _alunoService.AtualizarMatriculaPrefeituraAsync(alunoId, matriculaPrefeitura, cancellationToken);
        return resultado.Succeeded ? AtaResult.Success() : AtaResult.Fail(resultado.ErrorMessage ?? "Não foi possível salvar a matrícula.");
    }
}
