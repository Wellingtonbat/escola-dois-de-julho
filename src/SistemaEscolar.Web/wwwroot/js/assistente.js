// Assistente (Elson): janela de dúvidas no canto da tela. As respostas vêm de /Assistente (busca no manual, sem IA).
// A conversa fica guardada na sessão do navegador para não se perder ao trocar de tela.
(() => {
  const raiz = document.getElementById("assistente");
  if (!raiz) {
    return;
  }

  const endpoint = raiz.dataset.endpoint;
  const painel = document.getElementById("assistentePainel");
  const botao = document.getElementById("assistenteBotao");
  const corpo = document.getElementById("assistenteCorpo");
  const form = document.getElementById("assistenteForm");
  const campo = document.getElementById("assistentePergunta");
  const CHAVE = `assistente-conversa-v1-${raiz.dataset.usuario}`;

  let estado = { aberto: false, mensagens: [] };
  try {
    estado = JSON.parse(sessionStorage.getItem(CHAVE)) || estado;
  } catch {
    // Sem sessionStorage (modo privado, bloqueado): a conversa só não é lembrada entre as telas.
  }

  const salvar = () => {
    try {
      sessionStorage.setItem(CHAVE, JSON.stringify({ aberto: estado.aberto, mensagens: estado.mensagens.slice(-40) }));
    } catch {
      // Ignorado de propósito (ver acima).
    }
  };

  const escapar = (texto) =>
    String(texto ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);

  // Só **negrito** é formatado; todo o resto é escapado.
  const formatar = (texto) => escapar(texto).replace(/\*\*(.+?)\*\*/g, "<strong>$1</strong>");

  const chipPergunta = (sugestao) =>
    `<button type="button" class="assistente-chip" data-topico="${escapar(sugestao.id)}" data-texto="${escapar(sugestao.titulo)}">${escapar(sugestao.titulo)}</button>`;

  const rolarParaFim = () => {
    corpo.scrollTop = corpo.scrollHeight;
  };

  const desenharMensagem = (mensagem) => {
    const div = document.createElement("div");
    if (mensagem.de === "usuario") {
      div.className = "assistente-msg assistente-msg-usuario";
      div.textContent = mensagem.texto;
      corpo.appendChild(div);
      return;
    }

    const r = mensagem.resposta;
    div.className = "assistente-msg assistente-msg-elson";
    let html = `<div>${formatar(r.texto)}</div>`;
    if (r.passos?.length) {
      html += `<ol>${r.passos.map((p) => `<li>${formatar(p)}</li>`).join("")}</ol>`;
    }
    if (r.observacao) {
      html += `<p class="assistente-obs">${formatar(r.observacao)}</p>`;
    }
    if (r.aviso) {
      html += `<div class="assistente-aviso">⚠️ ${formatar(r.aviso)}</div>`;
    }

    const acoes = [];
    if (r.link) {
      acoes.push(`<a class="assistente-chip assistente-chip-principal" href="${escapar(r.link.pagina)}">${escapar(r.link.rotulo)} →</a>`);
    }
    if (r.secaoManual) {
      acoes.push(`<a class="assistente-chip" href="/Manual?handler=DownloadPdf">Manual, seção ${escapar(r.secaoManual)}</a>`);
    }
    if (acoes.length) {
      html += `<div class="assistente-acoes">${acoes.join("")}</div>`;
    }
    if (r.sugestoes?.length) {
      html += `<p class="assistente-rotulo">${r.encontrou ? "Talvez você esteja procurando:" : "Sugestões:"}</p>`;
      html += `<div class="assistente-acoes">${r.sugestoes.map(chipPergunta).join("")}</div>`;
    }

    div.innerHTML = html;
    corpo.appendChild(div);
  };

  const desenharBoasVindas = (boasVindas) => {
    const div = document.createElement("div");
    div.className = "assistente-msg assistente-msg-elson";
    div.innerHTML =
      `<div>${escapar(boasVindas.saudacao)} 👋</div>` +
      `<div class="assistente-obs">Seu perfil: <strong>${escapar(boasVindas.perfil)}</strong>. Pergunte como fazer qualquer coisa no sistema.</div>`;
    corpo.appendChild(div);
    if (boasVindas.frequentes?.length) {
      const rotulo = document.createElement("p");
      rotulo.className = "assistente-rotulo";
      rotulo.textContent = "Perguntas frequentes";
      corpo.appendChild(rotulo);
      const acoes = document.createElement("div");
      acoes.className = "assistente-acoes";
      acoes.innerHTML = boasVindas.frequentes.map(chipPergunta).join("");
      corpo.appendChild(acoes);
    }
  };

  const redesenhar = () => {
    corpo.innerHTML = "";
    if (estado.boasVindas) {
      desenharBoasVindas(estado.boasVindas);
    }
    estado.mensagens.forEach(desenharMensagem);
    rolarParaFim();
  };

  const buscar = async (parametros) => {
    const url = new URL(endpoint, window.location.origin);
    Object.entries(parametros).forEach(([chave, valor]) => {
      if (valor) {
        url.searchParams.set(chave, valor);
      }
    });
    const resposta = await fetch(url, { headers: { Accept: "application/json" }, credentials: "same-origin" });
    if (!resposta.ok || !resposta.headers.get("content-type")?.includes("json")) {
      throw new Error("resposta inválida");
    }
    return resposta.json();
  };

  const perguntar = async (texto, topico) => {
    estado.mensagens.push({ de: "usuario", texto });
    salvar();
    redesenhar();

    const digitando = document.createElement("div");
    digitando.className = "assistente-msg assistente-msg-elson assistente-digitando";
    digitando.textContent = "•••";
    corpo.appendChild(digitando);
    rolarParaFim();

    let resposta;
    try {
      resposta = await buscar({ handler: "Perguntar", q: topico ? null : texto, topico });
    } catch {
      resposta = { encontrou: false, texto: "Não consegui responder agora. Tente de novo em instantes.", passos: [], sugestoes: [] };
    }

    estado.mensagens.push({ de: "elson", resposta });
    salvar();
    redesenhar();
  };

  const abrir = async () => {
    estado.aberto = true;
    salvar();
    painel.hidden = false;
    botao.setAttribute("aria-expanded", "true");
    if (!estado.boasVindas) {
      try {
        estado.boasVindas = await buscar({ handler: "BoasVindas" });
        salvar();
      } catch {
        estado.boasVindas = { saudacao: `Olá! Me chamo ${raiz.dataset.nome}, sou seu assistente.`, perfil: "—", frequentes: [] };
      }
    }
    redesenhar();
    campo.focus();
  };

  const fechar = () => {
    estado.aberto = false;
    salvar();
    painel.hidden = true;
    botao.setAttribute("aria-expanded", "false");
    botao.focus();
  };

  botao.addEventListener("click", () => (painel.hidden ? abrir() : fechar()));
  raiz.querySelector("[data-assistente-fechar]").addEventListener("click", fechar);
  painel.addEventListener("keydown", (evento) => {
    if (evento.key === "Escape") {
      fechar();
    }
  });

  form.addEventListener("submit", (evento) => {
    evento.preventDefault();
    const texto = campo.value.trim();
    if (!texto) {
      return;
    }
    campo.value = "";
    perguntar(texto, null);
  });

  corpo.addEventListener("click", (evento) => {
    const chip = evento.target.closest("[data-topico]");
    if (chip) {
      perguntar(chip.dataset.texto, chip.dataset.topico);
    }
  });

  if (estado.aberto) {
    abrir();
  }
})();
