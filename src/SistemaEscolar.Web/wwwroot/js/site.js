const appShell = document.getElementById("appShell");
const sidebarToggle = document.getElementById("sidebarToggle");
const appOverlay = document.getElementById("appOverlay");
const SIDEBAR_COLLAPSED_KEY = "se.sidebarCollapsed";

if (appShell && sidebarToggle && appOverlay) {
  const closeSidebar = () => appShell.classList.remove("sidebar-open");
  const menuLinks = document.querySelectorAll(".sidebar-link");

  const setDesktopCollapsed = (collapsed) => {
    appShell.classList.toggle("sidebar-collapsed", collapsed);
    sidebarToggle.setAttribute("aria-expanded", (!collapsed).toString());
    sidebarToggle.setAttribute(
      "aria-label",
      collapsed ? "Exibir menu lateral" : "Ocultar menu lateral",
    );
  };

  try {
    setDesktopCollapsed(localStorage.getItem(SIDEBAR_COLLAPSED_KEY) === "true");
  } catch {
    setDesktopCollapsed(false);
  }

  sidebarToggle.addEventListener("click", () => {
    if (window.innerWidth >= 992) {
      const collapsed = !appShell.classList.contains("sidebar-collapsed");
      setDesktopCollapsed(collapsed);
      try {
        localStorage.setItem(SIDEBAR_COLLAPSED_KEY, collapsed.toString());
      } catch {
        // Ignora falhas de armazenamento local.
      }
      return;
    }

    appShell.classList.toggle("sidebar-open");
  });

  menuLinks.forEach((link) => {
    link.addEventListener("click", () => {
      if (window.innerWidth < 992) {
        closeSidebar();
      }
    });
  });

  appOverlay.addEventListener("click", closeSidebar);

  window.addEventListener("resize", () => {
    if (window.innerWidth >= 992) {
      closeSidebar();
    }
  });
}

document.querySelectorAll("tr.row-clicavel").forEach((row) => {
  row.addEventListener("click", (event) => {
    if (event.target.closest(".actions-cell")) {
      return;
    }

    const destino = row.dataset.href;
    if (destino) {
      window.location.href = destino;
      return;
    }

    const editSelector = row.dataset.editTrigger;
    if (editSelector) {
      row.querySelector(editSelector)?.click();
    }
  });
});

const systemFeedbackModalElement = document.getElementById(
  "systemFeedbackModal",
);
const systemFeedbackModalLabel = document.getElementById(
  "systemFeedbackModalLabel",
);
const systemFeedbackModalMessage = document.getElementById(
  "systemFeedbackModalMessage",
);

if (
  systemFeedbackModalElement &&
  systemFeedbackModalLabel &&
  systemFeedbackModalMessage &&
  window.bootstrap
) {
  const feedbackSuccess = document.body.dataset.flashSuccess;
  const feedbackError = document.body.dataset.flashError;

  if (feedbackSuccess || feedbackError) {
    const feedbackModal = new window.bootstrap.Modal(
      systemFeedbackModalElement,
    );

    if (feedbackError) {
      systemFeedbackModalLabel.textContent = "Falha na ação";
      systemFeedbackModalMessage.textContent = feedbackError;
    } else {
      systemFeedbackModalLabel.textContent = "Ação concluída";
      systemFeedbackModalMessage.textContent = feedbackSuccess;
    }

    feedbackModal.show();
  }
}

document.querySelectorAll("[data-cpf-mask]").forEach((input) => {
  const formatCpf = (digits) => {
    if (digits.length > 9) {
      return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6, 9)}-${digits.slice(9, 11)}`;
    }
    if (digits.length > 6) {
      return `${digits.slice(0, 3)}.${digits.slice(3, 6)}.${digits.slice(6)}`;
    }
    if (digits.length > 3) {
      return `${digits.slice(0, 3)}.${digits.slice(3)}`;
    }
    return digits;
  };

  input.setAttribute("inputmode", "numeric");
  input.setAttribute("maxlength", "14");
  input.addEventListener("input", () => {
    const digits = input.value.replace(/\D/g, "").slice(0, 11);
    input.value = formatCpf(digits);
  });
});

document.querySelectorAll("[data-password-toggle]").forEach((button) => {
  const input = document.getElementById(button.dataset.passwordToggle);
  if (!input) {
    return;
  }

  button.addEventListener("click", () => {
    const showing = input.type === "text";
    input.type = showing ? "password" : "text";
    button.classList.toggle("is-showing", !showing);
    button.setAttribute("aria-label", showing ? "Mostrar senha" : "Ocultar senha");
  });
});

document.querySelectorAll("[data-password-rules]").forEach((rulesList) => {
  const input = document.getElementById(rulesList.dataset.passwordRules);
  if (!input) {
    return;
  }

  const ruleLength = rulesList.querySelector('[data-rule="length"]');
  const ruleLetter = rulesList.querySelector('[data-rule="letter"]');
  const ruleDigit = rulesList.querySelector('[data-rule="digit"]');

  const validate = () => {
    const value = input.value;
    ruleLength?.classList.toggle("valid", value.length >= 8);
    ruleLetter?.classList.toggle("valid", /[a-zA-Z]/.test(value));
    ruleDigit?.classList.toggle("valid", /[0-9]/.test(value));
  };

  input.addEventListener("input", validate);
  validate();
});

// Multi-select com chips: transforma um <select multiple data-chip-select> num campo com os itens
// selecionados exibidos como chips removíveis e um painel de busca/seleção, sem mudar o que é
// enviado no formulário (o <select> original continua existindo, só fica visualmente oculto).
function initChipSelect(select) {
  if (select.dataset.chipSelectInit === "true") {
    return;
  }
  select.dataset.chipSelectInit = "true";

  const wrapper = document.createElement("div");
  wrapper.className = "chip-select";
  select.insertAdjacentElement("beforebegin", wrapper);

  const control = document.createElement("div");
  control.className = "chip-select-control";

  const chipsArea = document.createElement("div");
  chipsArea.className = "chip-select-chips";

  const searchInput = document.createElement("input");
  searchInput.type = "text";
  searchInput.className = "chip-select-search";
  searchInput.autocomplete = "off";

  control.appendChild(chipsArea);
  control.appendChild(searchInput);

  const panel = document.createElement("div");
  panel.className = "chip-select-panel";
  panel.hidden = true;

  wrapper.appendChild(control);
  wrapper.appendChild(panel);
  wrapper.appendChild(select);
  select.classList.add("chip-select-native");

  const options = () => Array.from(select.options);

  const toggleOption = (opt) => {
    opt.selected = !opt.selected;
    select.dispatchEvent(new Event("change", { bubbles: true }));
    searchInput.focus();
  };

  const renderPanel = (filtro) => {
    panel.innerHTML = "";
    const termo = (filtro || "").trim().toLowerCase();
    let algumVisivel = false;

    options().forEach((opt) => {
      if (termo && !opt.text.toLowerCase().includes(termo)) {
        return;
      }
      algumVisivel = true;

      const item = document.createElement("button");
      item.type = "button";
      item.className = "chip-select-item" + (opt.selected ? " is-selected" : "");
      item.textContent = opt.text;
      item.addEventListener("click", () => toggleOption(opt));
      panel.appendChild(item);
    });

    if (!algumVisivel) {
      const vazio = document.createElement("div");
      vazio.className = "chip-select-empty";
      vazio.textContent = "Nenhum resultado.";
      panel.appendChild(vazio);
    }
  };

  const renderChips = () => {
    chipsArea.innerHTML = "";
    const selecionados = options().filter((opt) => opt.selected);

    selecionados.forEach((opt) => {
      const chip = document.createElement("span");
      chip.className = "chip-select-chip";

      const label = document.createElement("span");
      label.textContent = opt.text;

      const remover = document.createElement("button");
      remover.type = "button";
      remover.className = "chip-select-remove";
      remover.setAttribute("aria-label", `Remover ${opt.text}`);
      remover.textContent = "×";
      remover.addEventListener("click", (evento) => {
        evento.stopPropagation();
        toggleOption(opt);
      });

      chip.appendChild(label);
      chip.appendChild(remover);
      chipsArea.appendChild(chip);
    });

    searchInput.placeholder = selecionados.length ? "" : (select.dataset.chipPlaceholder || "Selecione...");
  };

  const render = () => {
    renderChips();
    renderPanel(searchInput.value);
  };

  const abrirPainel = () => {
    panel.hidden = false;
    wrapper.classList.add("is-open");
    renderPanel(searchInput.value);
  };

  const fecharPainel = () => {
    panel.hidden = true;
    wrapper.classList.remove("is-open");
    searchInput.value = "";
    renderPanel("");
  };

  control.addEventListener("click", () => {
    searchInput.focus();
    abrirPainel();
  });

  searchInput.addEventListener("focus", abrirPainel);
  searchInput.addEventListener("input", () => renderPanel(searchInput.value));
  searchInput.addEventListener("keydown", (evento) => {
    if (evento.key === "Escape") {
      fecharPainel();
      searchInput.blur();
    }
    if (evento.key === "Backspace" && !searchInput.value) {
      const selecionados = options().filter((opt) => opt.selected);
      const ultimo = selecionados[selecionados.length - 1];
      if (ultimo) {
        toggleOption(ultimo);
      }
    }
  });

  document.addEventListener("click", (evento) => {
    // composedPath() (não evento.target/wrapper.contains) porque o clique num item do painel
    // recria o painel (innerHTML) dentro do próprio handler — na hora em que este listener roda na
    // fase de bubbling, o nó original já foi removido da árvore. composedPath() fica congelado no
    // momento do dispatch, então continua incluindo wrapper mesmo com o nó já desconectado.
    const path = typeof evento.composedPath === "function" ? evento.composedPath() : [];
    if (!path.includes(wrapper)) {
      fecharPainel();
    }
  });

  select.addEventListener("change", render);

  render();
}

document.querySelectorAll("select[multiple][data-chip-select]").forEach(initChipSelect);
