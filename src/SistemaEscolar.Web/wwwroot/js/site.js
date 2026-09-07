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
