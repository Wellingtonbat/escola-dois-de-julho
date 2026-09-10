const dashKpis = document.getElementById("dash-kpis");

if (dashKpis) {
  const dashBarras = document.getElementById("dash-barras");
  const dashDonut = document.getElementById("dash-donut");
  const dashLinha = document.getElementById("dash-linha");
  const dashRanking = document.getElementById("dash-ranking");
  const dashHeatmap = document.getElementById("dash-heatmap");
  const filtroProfessor = document.getElementById("dash-professor");
  const filtroTurma = document.getElementById("dash-turma");
  const filtroDisciplina = document.getElementById("dash-disciplina");
  const filtroAno = document.getElementById("dash-ano");
  const filtroTrimestre = document.getElementById("dash-trimestre");
  const botaoFiltrar = document.getElementById("dash-filtrar");
  const botaoLimpar = document.getElementById("dash-limpar");

  const ICON_ALUNOS =
    '<svg viewBox="0 0 16 16" fill="currentColor"><path d="M8 1 15 4.5 8 8 1 4.5 8 1Z"/><path d="M4 6.2v3.3c0 .9 1.8 1.8 4 1.8s4-.9 4-1.8V6.2L8 8 4 6.2Z"/><path d="M14 6v3.5a.5.5 0 0 0 1 0V6h-1Z"/></svg>';
  const ICON_CHECK =
    '<svg viewBox="0 0 16 16" fill="currentColor"><path d="M8 15A7 7 0 1 0 8 1a7 7 0 0 0 0 14Zm3.36-9.14-4 4a.5.5 0 0 1-.72 0l-2-2a.5.5 0 1 1 .72-.72L7 8.79l3.64-3.65a.5.5 0 0 1 .72.72Z"/></svg>';
  const ICON_ALERTA =
    '<svg viewBox="0 0 16 16" fill="currentColor"><path d="M8.982 1.566a1.13 1.13 0 0 0-1.964 0L.165 13.233c-.457.778.091 1.767.982 1.767h13.706c.891 0 1.439-.99.982-1.767zM8 5c.535 0 .954.462.9.995l-.35 3.507a.552.552 0 0 1-1.1 0L7.1 5.995A.905.905 0 0 1 8 5zm.002 6a1 1 0 1 1 0 2 1 1 0 0 1 0-2z"/></svg>';
  const ICON_CALENDARIO =
    '<svg viewBox="0 0 16 16" fill="currentColor"><path d="M4 .5a.5.5 0 0 0-1 0V1H2a2 2 0 0 0-2 2v11a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V3a2 2 0 0 0-2-2h-1V.5a.5.5 0 0 0-1 0V1H4V.5ZM1 4h14v10a1 1 0 0 1-1 1H2a1 1 0 0 1-1-1V4Z"/></svg>';

  const PALETA_BARRAS = ["#3267f6", "#00a7d1", "#ff8a34", "#1fb980", "#ef4ea8", "#f2b400"];

  function formatarDecimal(valor, casas) {
    if (valor === null || valor === undefined) return "—";
    return valor.toLocaleString("pt-BR", { minimumFractionDigits: casas, maximumFractionDigits: casas });
  }

  function formatarPercentual(valor) {
    return `${formatarDecimal(valor, 1)}%`;
  }

  function escaparHtml(texto) {
    const div = document.createElement("div");
    div.textContent = texto ?? "";
    return div.innerHTML;
  }

  function descreverEscopo(dados, { comTurma, comDisciplina, comProfessor }) {
    const partes = [];
    if (comTurma) partes.push(dados.turmaSelecionadaNome ?? "Todas as turmas");
    if (comDisciplina) partes.push(dados.disciplinaSelecionadaNome ?? "Todas as disciplinas");
    if (comProfessor && dados.professorSelecionadoNome) partes.push(dados.professorSelecionadoNome);
    partes.push(dados.trimestre ? `${dados.trimestre}º Trimestre/${dados.anoLetivo}` : `Ano letivo ${dados.anoLetivo} (completo)`);
    return partes.join(" · ");
  }

  function renderKpis(dados) {
    document.getElementById("dash-kpis-caption").textContent = `Visão geral da escola — ano letivo ${dados.anoLetivo}`;
    const k = dados.kpis;
    dashKpis.innerHTML = `
      <article class="kpi-card kpi-card-icon">
        <div><span class="kpi-label">Alunos matriculados</span><strong class="kpi-value">${k.totalAlunos}</strong></div>
        <span class="kpi-icon">${ICON_ALUNOS}</span>
      </article>
      <article class="kpi-card kpi-card-icon">
        <div><span class="kpi-label">Aprovação geral</span><strong class="kpi-value">${formatarPercentual(k.percentualAprovacaoGeral)}</strong></div>
        <span class="kpi-icon">${ICON_CHECK}</span>
      </article>
      <article class="kpi-card kpi-card-icon">
        <div><span class="kpi-label">Pendências de lançamento</span><strong class="kpi-value">${k.pendenciasLancamento}</strong></div>
        <span class="kpi-icon">${ICON_ALERTA}</span>
      </article>
      <article class="kpi-card kpi-card-icon">
        <div><span class="kpi-label">Períodos em aberto</span><strong class="kpi-value">${k.periodosAbertos} <span class="small text-muted" style="font-weight:600">/ ${k.totalPeriodos}</span></strong></div>
        <span class="kpi-icon">${ICON_CALENDARIO}</span>
      </article>`;
  }

  function renderBarras(dados) {
    document.getElementById("dash-barras-caption").textContent = dados.turmaSelecionadaNome
      ? descreverEscopo(dados, { comTurma: true, comProfessor: true })
      : "Selecione uma turma no filtro acima para comparar as disciplinas";

    if (!dados.mediasPorDisciplina || dados.mediasPorDisciplina.length === 0) {
      dashBarras.innerHTML = dados.turmaSelecionadaNome
        ? '<p class="chart-empty-state">Nenhum resultado lançado para esta turma ainda.</p>'
        : '<p class="chart-empty-state">Selecione uma turma para ver a média por disciplina.</p>';
      return;
    }

    const barras = dados.mediasPorDisciplina
      .map((item, indice) => {
        const altura = Math.max(0, Math.min(100, (item.media / 10) * 100));
        const cor = PALETA_BARRAS[indice % PALETA_BARRAS.length];
        return `
          <div class="bar-col">
            <span class="bar-col-value">${formatarDecimal(item.media, 1)}</span>
            <div class="bar-col-fill" style="height:${altura}%;background:${cor}"></div>
            <span class="bar-col-label">${escaparHtml(item.disciplina)}</span>
          </div>`;
      })
      .join("");

    dashBarras.innerHTML = `
      <div class="bar-chart">
        <div class="bar-chart-threshold" style="bottom:calc(1.6rem + 50%)"></div>
        <span class="bar-chart-threshold-label" style="bottom:calc(1.6rem + 50%)">Média mínima 5,0</span>
        ${barras}
      </div>`;
  }

  function renderDonut(dados) {
    document.getElementById("dash-donut-caption").textContent = descreverEscopo(dados, {
      comTurma: true,
      comDisciplina: true,
      comProfessor: true,
    });

    const d = dados.donut;
    if (!d || d.total === 0) {
      dashDonut.innerHTML = '<p class="chart-empty-state">Nenhum resultado lançado para este filtro ainda.</p>';
      return;
    }

    const circunferencia = 263.9;
    const pctAprovados = (d.aprovados / d.total) * 100;
    const pctReprovados = (d.reprovados / d.total) * 100;
    const pctPendentes = (d.pendentes / d.total) * 100;

    const arcoAprovados = (pctAprovados / 100) * circunferencia;
    const arcoReprovados = (pctReprovados / 100) * circunferencia;
    const arcoPendentes = (pctPendentes / 100) * circunferencia;

    dashDonut.innerHTML = `
      <div class="donut-block">
        <svg width="120" height="120" viewBox="0 0 100 100">
          <circle cx="50" cy="50" r="42" fill="none" stroke="#eef0f6" stroke-width="12"/>
          <circle cx="50" cy="50" r="42" fill="none" stroke-width="12" stroke-linecap="round" transform="rotate(-90 50 50)"
                  stroke="#1fb980" stroke-dasharray="${arcoAprovados} ${circunferencia}" stroke-dashoffset="0"/>
          <circle cx="50" cy="50" r="42" fill="none" stroke-width="12" stroke-linecap="round" transform="rotate(-90 50 50)"
                  stroke="#ef4ea8" stroke-dasharray="${arcoReprovados} ${circunferencia}" stroke-dashoffset="${-arcoAprovados}"/>
          <circle cx="50" cy="50" r="42" fill="none" stroke-width="12" stroke-linecap="round" transform="rotate(-90 50 50)"
                  stroke="#f2b400" stroke-dasharray="${arcoPendentes} ${circunferencia}" stroke-dashoffset="${-(arcoAprovados + arcoReprovados)}"/>
          <text x="50" y="47" text-anchor="middle" font-size="17" font-weight="800" fill="#2b2c40" font-family="Public Sans, sans-serif">${formatarDecimal(pctAprovados, 0)}%</text>
          <text x="50" y="61" text-anchor="middle" font-size="7" fill="#6f7186" font-family="Public Sans, sans-serif">aprovação</text>
        </svg>
        <div>
          <p class="donut-legend-title">${d.total} registro(s)</p>
          <div class="legend-row"><span class="legend-dot" style="background:#1fb980"></span>Aprovados&nbsp;<b>${d.aprovados} (${formatarPercentual(pctAprovados)})</b></div>
          <div class="legend-row"><span class="legend-dot" style="background:#ef4ea8"></span>Reprovados&nbsp;<b>${d.reprovados} (${formatarPercentual(pctReprovados)})</b></div>
          <div class="legend-row"><span class="legend-dot" style="background:#f2b400"></span>Pendentes&nbsp;<b>${d.pendentes} (${formatarPercentual(pctPendentes)})</b></div>
        </div>
      </div>`;
  }

  function renderLinha(dados) {
    document.getElementById("dash-linha-caption").textContent = descreverEscopo(dados, {
      comTurma: true,
      comDisciplina: true,
      comProfessor: true,
    });

    const pontos = dados.evolucaoTrimestres ?? [];
    if (pontos.every((p) => p.media === null)) {
      dashLinha.innerHTML = '<p class="chart-empty-state">Ainda não há notas lançadas neste ano letivo.</p>';
      return;
    }

    const xs = [40, 200, 360];
    const escalaY = (media) => 160 - (media / 10) * 140;
    const validos = pontos
      .map((p, indice) => (p.media === null ? null : { x: xs[indice], y: escalaY(p.media), media: p.media, trimestre: p.trimestre }))
      .filter((p) => p !== null);

    const linha = validos.map((p) => `${p.x},${p.y.toFixed(1)}`).join(" L");
    const areaInicio = validos.length > 0 ? `M${validos[0].x},160 L` : "";
    const areaFim = validos.length > 0 ? ` L${validos[validos.length - 1].x},160 Z` : "";

    const circulos = validos
      .map((p) => `<circle cx="${p.x}" cy="${p.y.toFixed(1)}" r="5" fill="#fff" stroke="#3267f6" stroke-width="3"/>`)
      .join("");
    const rotulos = validos
      .map((p) => `<text x="${p.x}" y="${(p.y - 14).toFixed(1)}" text-anchor="middle" class="line-point-label" fill="#2b2c40" font-family="Public Sans, sans-serif">${formatarDecimal(p.media, 1)}</text>`)
      .join("");
    const rotulosEixo = xs
      .map((x, indice) => `<text x="${x}" y="178" text-anchor="middle" font-size="10" fill="#6f7186" font-family="Public Sans, sans-serif">${indice + 1}º Trim.</text>`)
      .join("");

    dashLinha.innerHTML = `
      <svg width="100%" height="200" viewBox="0 0 400 200" preserveAspectRatio="none">
        <defs>
          <linearGradient id="dashLineFill" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stop-color="#3267f6" stop-opacity="0.28"/>
            <stop offset="100%" stop-color="#3267f6" stop-opacity="0"/>
          </linearGradient>
        </defs>
        <line x1="20" y1="160" x2="380" y2="160" stroke="#e6e8f0" stroke-width="1"/>
        <line x1="20" y1="20" x2="20" y2="160" stroke="#e6e8f0" stroke-width="1"/>
        <line x1="20" y1="90" x2="380" y2="90" stroke="#d8bfe8" stroke-width="1.4" stroke-dasharray="4 4"/>
        <text x="384" y="93" font-size="9" fill="#8b8ea3" font-family="Public Sans, sans-serif">5,0</text>
        ${validos.length > 0 ? `<path d="${areaInicio}${linha}${areaFim}" fill="url(#dashLineFill)"/>` : ""}
        ${validos.length > 1 ? `<path d="M${linha}" fill="none" stroke="#3267f6" stroke-width="3" stroke-linecap="round" stroke-linejoin="round"/>` : ""}
        ${circulos}
        ${rotulos}
        ${rotulosEixo}
      </svg>`;
  }

  function renderRankingColuna(titulo, corTitulo, icone, itens, corBarra) {
    if (itens.length === 0) {
      return `<div><p class="ranking-col-title" style="color:${corTitulo}">${icone}${titulo}</p><p class="chart-empty-state">Sem dados.</p></div>`;
    }

    const linhas = itens
      .map(
        (item, indice) => `
        <div class="ranking-item">
          <span class="ranking-rank" style="background:${corBarra}">${indice + 1}</span>
          <span class="ranking-name" title="${escaparHtml(item.turma)}">${escaparHtml(item.turma)}</span>
          <span class="ranking-track"><span class="ranking-fill" style="width:${item.percentualAprovacao}%;background:${corBarra}"></span></span>
          <span class="ranking-pct" style="color:${corTitulo}">${formatarPercentual(item.percentualAprovacao)}</span>
        </div>`,
      )
      .join("");

    return `<div><p class="ranking-col-title" style="color:${corTitulo}">${icone}${titulo}</p>${linhas}</div>`;
  }

  function renderRanking(dados) {
    document.getElementById("dash-ranking-caption").textContent = descreverEscopo(dados, {
      comDisciplina: true,
      comProfessor: true,
    });

    const melhores = dados.rankingMelhores ?? [];
    const atencao = dados.rankingAtencao ?? [];

    if (melhores.length === 0 && atencao.length === 0) {
      dashRanking.innerHTML = '<p class="chart-empty-state">Nenhuma turma com resultados neste filtro.</p>';
      return;
    }

    dashRanking.innerHTML = `
      <div class="ranking-cols">
        ${renderRankingColuna("Melhor desempenho", "#107149", ICON_CHECK, melhores, "#1fb980")}
        ${renderRankingColuna("Atenção necessária", "#b42356", ICON_ALERTA, atencao, "#ef4ea8")}
      </div>`;
  }

  function renderHeatmap(dados) {
    document.getElementById("dash-heatmap-caption").textContent = descreverEscopo(dados, { comProfessor: true });

    const turmas = dados.heatmapTurmas ?? [];
    const disciplinas = dados.heatmapDisciplinas ?? [];
    const celulas = dados.heatmapCelulas ?? [];

    if (turmas.length === 0 || disciplinas.length === 0) {
      dashHeatmap.innerHTML = '<p class="chart-empty-state">Nenhuma turma/disciplina disponível para este filtro.</p>';
      return;
    }

    const mapa = new Map(celulas.map((c) => [`${c.turma}||${c.disciplina}`, c]));

    const cabecalho = disciplinas.map((d) => `<th>${escaparHtml(d)}</th>`).join("");
    const linhas = turmas
      .map((turma) => {
        const colunas = disciplinas
          .map((disciplina) => {
            const celula = mapa.get(`${turma}||${disciplina}`);
            if (!celula) {
              return '<td><div class="heatmap-cell" style="background:#f1f1f5;color:#b7b9c8">—</div></td>';
            }
            if (celula.esperadas === 0) {
              return '<td><div class="heatmap-cell" style="background:#f1f1f5;color:#b7b9c8">—<small>sem alunos ativos</small></div></td>';
            }
            const pendentes = celula.esperadas - celula.lancadas;
            const ratio = celula.lancadas / celula.esperadas;
            const cor = ratio >= 1 ? { bg: "#ddf8ec", fg: "#107149" } : ratio >= 0.7 ? { bg: "#fff1d7", fg: "#9b5f00" } : { bg: "#ffe3ea", fg: "#b42356" };
            const detalhe = pendentes > 0 ? `<small>${pendentes} pendente(s)</small>` : "";
            return `<td><div class="heatmap-cell" style="background:${cor.bg};color:${cor.fg}">${celula.lancadas}/${celula.esperadas}${detalhe}</div></td>`;
          })
          .join("");
        return `<tr><td class="heatmap-row-label">${escaparHtml(turma)}</td>${colunas}</tr>`;
      })
      .join("");

    dashHeatmap.innerHTML = `
      <div class="heatmap-scroll">
        <table class="heatmap-table">
          <thead><tr><th></th>${cabecalho}</tr></thead>
          <tbody>${linhas}</tbody>
        </table>
      </div>
      <div class="heatmap-legend">
        <span class="legend-row"><span class="legend-dot" style="background:#1fb980"></span>Completo — todas as notas lançadas</span>
        <span class="legend-row"><span class="legend-dot" style="background:#f2b400"></span>Parcial — até 30% pendente</span>
        <span class="legend-row"><span class="legend-dot" style="background:#ef4ea8"></span>Crítico — mais de 30% pendente</span>
      </div>`;
  }

  function renderizarDashboard(dados) {
    renderKpis(dados);
    renderBarras(dados);
    renderDonut(dados);
    renderLinha(dados);
    renderRanking(dados);
    renderHeatmap(dados);
  }

  async function carregarDados() {
    const params = new URLSearchParams();
    params.set("anoLetivo", filtroAno.value);
    if (filtroTrimestre.value) params.set("trimestre", filtroTrimestre.value);
    if (filtroProfessor.value) params.set("professorId", filtroProfessor.value);
    if (filtroTurma.value) params.set("turmaId", filtroTurma.value);
    if (filtroDisciplina.value) params.set("disciplinaId", filtroDisciplina.value);

    try {
      const resposta = await fetch(`?handler=Dados&${params.toString()}`, {
        headers: { "X-Requested-With": "XMLHttpRequest" },
      });
      if (!resposta.ok) return;
      const dados = await resposta.json();
      renderizarDashboard(dados);
    } catch (erro) {
      console.error(erro);
    }
  }

  botaoFiltrar?.addEventListener("click", carregarDados);
  botaoLimpar?.addEventListener("click", () => {
    filtroProfessor.value = "";
    filtroTurma.value = "";
    filtroDisciplina.value = "";
    filtroTrimestre.value = "";
    carregarDados();
  });

  const dadosIniciaisEl = document.getElementById("dash-dados-iniciais");
  const dadosIniciais = dadosIniciaisEl ? JSON.parse(dadosIniciaisEl.textContent) : null;
  if (dadosIniciais) {
    renderizarDashboard(dadosIniciais);
  } else {
    carregarDados();
  }
}
