using SistemaEscolar.Application.Abstractions;
using SistemaEscolar.Application.Alunos;
using SistemaEscolar.Application.Disciplinas;
using SistemaEscolar.Application.Resultados;
using SistemaEscolar.Application.Series;
using SistemaEscolar.Application.Turmas;
using SistemaEscolar.Application.Usuarios;
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
    private readonly IUsuarioService _usuarioService;
    private readonly ISerieService _serieService;

    public AtaService(
        IAtaRepository ataRepository,
        ITurmaService turmaService,
        IAlunoService alunoService,
        IResultadoAcademicoService resultadoAcademicoService,
        IDisciplinaService disciplinaService,
        ICurrentUserService currentUserService,
        IUsuarioService usuarioService,
        ISerieService serieService)
    {
        _ataRepository = ataRepository;
        _turmaService = turmaService;
        _alunoService = alunoService;
        _resultadoAcademicoService = resultadoAcademicoService;
        _disciplinaService = disciplinaService;
        _currentUserService = currentUserService;
        _usuarioService = usuarioService;
        _serieService = serieService;
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

        var ata = new Ata
        {
            Id = Guid.NewGuid(),
            TurmaId = turmaId,
            Status = AtaConstantes.StatusRascunho
        };

        // As linhas (uma por aluno da turma) são criadas pela montagem do rascunho, a mesma usada ao abrir a Ata.
        await _ataRepository.CriarComItensAsync(ata, Array.Empty<(AtaAluno, IReadOnlyList<AtaAlunoDisciplina>)>(), cancellationToken);
        await MontarRascunhoAsync(ata, turma, cancellationToken);
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

        var montagem = ata.Status == AtaConstantes.StatusRascunho
            ? await MontarRascunhoAsync(ata, turma, cancellationToken)
            : await MontarFinalizadaAsync(ata, turma, cancellationToken);

        var series = await _serieService.ListarAsync(null, cancellationToken);

        var itens = montagem.Linhas
            .OrderBy(x => x.Aluno?.NomeCompleto ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select((linha, indice) =>
            {
                var semNotas = AtaConstantes.EhRFManual(linha.Item.RF);
                return new AtaAlunoItemDto(
                    linha.Item.Id,
                    linha.Item.AlunoId,
                    indice + 1,
                    linha.Aluno?.NomeCompleto ?? "Aluno não encontrado",
                    linha.Aluno?.MatriculaPrefeitura,
                    linha.Aluno?.IsAtivo ?? false,
                    semNotas ? Array.Empty<AtaAlunoDisciplinaDto>() : linha.Disciplinas,
                    linha.Item.RF,
                    linha.Item.RF == AtaConstantes.RFPendente ? linha.Pendencia : null,
                    AtaCalculo.AptoACursar(linha.Item.RF, turma.SerieId, series));
            })
            .ToList();

        var podeEditar = ata.Status == AtaConstantes.StatusRascunho
            ? PermissoesPerfil.PodeAcessarAtas(_currentUserService.IsInRole)
            : PermissoesPerfil.PodeEditarAtaFinalizada(_currentUserService.IsInRole);

        var diretorNome = await ObterDiretorNomeAsync(cancellationToken);

        return new AtaDetalheDto(
            ata.Id,
            ata.TurmaId,
            turma.Nome,
            turma.SerieNome,
            turma.AnoLetivo,
            turma.Turno,
            diretorNome,
            ata.Status,
            podeEditar,
            ata.FinalizadaEmUtc,
            ata.FinalizadaPorNome,
            montagem.Colunas,
            itens);
    }

    private sealed record LinhaAta(AtaAluno Item, AlunoListItemDto? Aluno, IReadOnlyList<AtaAlunoDisciplinaDto> Disciplinas, string? Pendencia);

    private sealed record MontagemAta(IReadOnlyList<AtaDisciplinaColunaDto> Colunas, IReadOnlyList<LinhaAta> Linhas);

    // Enquanto a Ata está em Rascunho, ela reflete sempre a situação atual da turma: notas e Avaliação Final
    // lançadas depois da criação da Ata entram na hora, alunos que entraram na turma ganham uma linha e quem
    // saiu da turma deixa de aparecer. Alunos inativos (transferidos/desistentes) entram como Transferido(a).
    // O resultado recalculado é gravado na linha para a lista de Atas mostrar o andamento.
    private async Task<MontagemAta> MontarRascunhoAsync(Ata ata, TurmaListItemDto turma, CancellationToken cancellationToken)
    {
        var alunosDaTurma = await _alunoService.ListarAsync(new AlunoListFilter(null, null, turma.Id, null), cancellationToken);
        var idsDaTurma = alunosDaTurma.Select(x => x.Id).ToHashSet();

        var resultados = (await _resultadoAcademicoService.ListarAsync(
                new ResultadoAcademicoFilter(turma.AnoLetivo, turma.Nome, null, "todos"), cancellationToken))
            .Where(x => idsDaTurma.Contains(x.AlunoId))
            .ToList();
        var resultadosPorAluno = resultados.ToLookup(x => x.AlunoId);
        var disciplinasComLancamento = resultados.Select(x => x.DisciplinaId).ToHashSet();

        var colunas = await MontarColunasAsync(turma.SerieId, disciplinasComLancamento, cancellationToken);

        var existentes = await _ataRepository.GetItensAsync(ata.Id, cancellationToken);
        var existentesPorAluno = existentes.GroupBy(x => x.AlunoId).ToDictionary(g => g.Key, g => g.First());

        var novos = new List<AtaAluno>();
        var atualizados = new List<AtaAluno>();
        var linhas = new List<LinhaAta>();

        foreach (var aluno in alunosDaTurma)
        {
            var item = existentesPorAluno.GetValueOrDefault(aluno.Id);
            if (item is null)
            {
                item = new AtaAluno
                {
                    Id = Guid.NewGuid(),
                    AtaId = ata.Id,
                    AlunoId = aluno.Id,
                    MC = AtaConstantes.MediaAprovacao,
                    RF = aluno.IsAtivo ? AtaConstantes.RFPendente : AtaConstantes.RFTransferido
                };
                novos.Add(item);
            }

            var rfAnterior = item.RF;
            var tpAnterior = item.TP;

            AtaCalculo.ResultadoAluno calculo;
            if (!aluno.IsAtivo && !AtaConstantes.EhRFManual(item.RF))
            {
                // Aluno inativado depois de a linha existir: passa a Transferido(a) (pode ser trocado à mão).
                item.RF = AtaConstantes.RFTransferido;
            }

            if (AtaConstantes.EhRFManual(item.RF))
            {
                calculo = new AtaCalculo.ResultadoAluno(Array.Empty<AtaAlunoDisciplinaDto>(), item.RF, null);
            }
            else
            {
                calculo = AtaCalculo.Calcular(colunas, resultadosPorAluno[aluno.Id].ToList(), disciplinasComLancamento);
                item.RF = calculo.RF;
            }

            item.TP = Math.Round(calculo.Disciplinas.Sum(d => d.ResultadoFinalAno), 2);

            if (!novos.Contains(item) && (item.RF != rfAnterior || item.TP != tpAnterior))
            {
                atualizados.Add(item);
            }

            linhas.Add(new LinhaAta(item, aluno, calculo.Disciplinas, calculo.Pendencia));
        }

        var removidos = existentes.Where(x => !idsDaTurma.Contains(x.AlunoId)).ToList();
        await _ataRepository.SincronizarItensAsync(novos, atualizados, removidos, cancellationToken);

        return new MontagemAta(colunas, linhas);
    }

    // Ata finalizada: mostra exatamente a "foto" gravada no fechamento (notas por disciplina e resultado).
    private async Task<MontagemAta> MontarFinalizadaAsync(Ata ata, TurmaListItemDto turma, CancellationToken cancellationToken)
    {
        var itens = await _ataRepository.GetItensAsync(ata.Id, cancellationToken);
        var disciplinasPorItem = (await _ataRepository.GetDisciplinasAsync(itens.Select(x => x.Id).ToList(), cancellationToken))
            .ToLookup(x => x.AtaAlunoId);
        var alunos = (await _alunoService.ListarAsync(null, cancellationToken)).ToDictionary(x => x.Id);

        var colunas = (await MontarColunasAsync(turma.SerieId, new HashSet<Guid>(), cancellationToken)).ToList();
        foreach (var gravada in disciplinasPorItem.SelectMany(g => g).GroupBy(x => x.DisciplinaId))
        {
            if (colunas.All(c => c.DisciplinaId != gravada.Key))
            {
                colunas.Add(new AtaDisciplinaColunaDto(gravada.Key, gravada.First().DisciplinaNome, gravada.First().DisciplinaCodigo));
            }
        }

        var linhas = itens
            .Select(item => new LinhaAta(
                item,
                alunos.GetValueOrDefault(item.AlunoId),
                disciplinasPorItem[item.Id]
                    .Select(x => new AtaAlunoDisciplinaDto(x.DisciplinaId, x.DisciplinaNome, x.DisciplinaCodigo, x.ResultadoFinalAno))
                    .ToList(),
                null))
            .ToList();

        return new MontagemAta(colunas.OrderBy(x => x.Nome, StringComparer.OrdinalIgnoreCase).ToList(), linhas);
    }

    // Colunas da Ata: as disciplinas ativas da série da turma (inclusive as sem nenhuma nota, que saem como
    // "—", como no modelo oficial), mais qualquer outra disciplina que tenha lançamento na turma.
    private async Task<IReadOnlyList<AtaDisciplinaColunaDto>> MontarColunasAsync(
        Guid serieId,
        IReadOnlySet<Guid> disciplinasComLancamento,
        CancellationToken cancellationToken)
    {
        return (await _disciplinaService.ListarAsync(null, cancellationToken))
            .Where(d => (d.IsAtiva && d.Series.Any(s => s.SerieId == serieId)) || disciplinasComLancamento.Contains(d.Id))
            .OrderBy(d => d.Nome, StringComparer.OrdinalIgnoreCase)
            .Select(d => new AtaDisciplinaColunaDto(d.Id, d.Nome, d.Codigo))
            .ToList();
    }

    // Usada no cabeçalho da Ata em PDF. Prioriza o Diretor; sem Diretor cadastrado, usa o Vice-Diretor
    // (mesmas permissões). Se houver mais de um, usa o primeiro por nome.
    private async Task<string?> ObterDiretorNomeAsync(CancellationToken cancellationToken)
    {
        var usuarios = await _usuarioService.ListarAsync(cancellationToken);
        var diretor = usuarios.FirstOrDefault(x => x.Perfil == Perfis.Diretor)
            ?? usuarios.FirstOrDefault(x => x.Perfil == Perfis.ViceDiretor);
        return diretor?.NomeCompleto;
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

    // Só Transferido(a) e Deixou de frequentar são escolhidos à mão; "Automático" devolve o aluno ao
    // resultado calculado pelo sistema (recalculado ao recarregar a Ata). Aluno inativo não volta ao automático.
    public async Task<AtaResult> DefinirRFManualAsync(Guid ataAlunoId, string rf, CancellationToken cancellationToken = default)
    {
        if (!AtaConstantes.EhRFManual(rf) && rf != AtaConstantes.RFAutomatico)
        {
            return AtaResult.Fail("Resultado inválido.");
        }

        var contexto = await CarregarItemEditavelAsync(ataAlunoId, cancellationToken);
        if (!contexto.Result.Succeeded)
        {
            return contexto.Result;
        }

        var item = contexto.Item!;

        // Ata finalizada é uma "foto" e não é recalculada: para mudar o resultado, é preciso antes
        // "Habilitar para Edição" (volta a Rascunho e passa a acompanhar as notas de novo).
        var ata = await _ataRepository.GetByIdAsync(item.AtaId, cancellationToken);
        if (ata?.Status == AtaConstantes.StatusFinalizada)
        {
            return AtaResult.Fail("Ata finalizada: use \"Habilitar para Edição\" antes de alterar o resultado.");
        }

        if (rf == AtaConstantes.RFAutomatico)
        {
            var aluno = await _alunoService.ObterPorIdAsync(item.AlunoId, cancellationToken);
            if (aluno is null || !aluno.IsAtivo)
            {
                return AtaResult.Fail("Aluno inativo: o resultado deve ser Transferido(a) ou Deixou de frequentar.");
            }

            item.RF = AtaConstantes.RFPendente;
        }
        else
        {
            item.RF = rf;
        }

        await _ataRepository.AtualizarItemAsync(item, cancellationToken);
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

        var turma = await _turmaService.ObterPorIdAsync(ata.TurmaId, cancellationToken);
        if (turma is null)
        {
            return AtaResult.Fail("Turma não encontrada.");
        }

        // Recalcula na hora de fechar, para a "foto" oficial sair com as notas mais recentes.
        var montagem = await MontarRascunhoAsync(ata, turma, cancellationToken);
        if (montagem.Linhas.Count == 0 || montagem.Linhas.Any(x => x.Item.RF == AtaConstantes.RFPendente))
        {
            return AtaResult.Fail("Ainda há aluno(s) sem resultado final definido.");
        }

        var disciplinas = montagem.Linhas
            .SelectMany(linha => linha.Disciplinas.Select(d => new AtaAlunoDisciplina
            {
                Id = Guid.NewGuid(),
                AtaAlunoId = linha.Item.Id,
                DisciplinaId = d.DisciplinaId,
                DisciplinaNome = d.DisciplinaNome,
                DisciplinaCodigo = d.DisciplinaCodigo,
                ResultadoFinalAno = d.ResultadoFinalAno
            }))
            .ToList();

        ata.Status = AtaConstantes.StatusFinalizada;
        ata.FinalizadaEmUtc = DateTime.UtcNow;
        ata.FinalizadaPorUserId = _currentUserService.UserId;
        ata.FinalizadaPorNome = _currentUserService.FullName ?? _currentUserService.UserName;

        await _ataRepository.FinalizarComResultadosAsync(ata, montagem.Linhas.Select(x => x.Item).ToList(), disciplinas, cancellationToken);
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
