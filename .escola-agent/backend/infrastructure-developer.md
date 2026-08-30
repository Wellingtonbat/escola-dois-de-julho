# AGENT: INFRASTRUCTURE DEVELOPER

## Missão

Implementar detalhes técnicos da aplicação.

---

## Responsabilidades

Criar:

- Repositories
- Persistence
- Identity
- Audit
- Excel
- Storage
- Email

---

## Estrutura

Infrastructure

Persistence

Repositories

Identity

Audit

Excel

Storage

Logging

---

## DbContext

Responsável por:

- Configurações
- Mapeamentos
- Relacionamentos
- Migrations

---

## Fluent API

Obrigatório.

Evitar dependência excessiva de DataAnnotations.

---

## Identity

Implementar:

- Login
- Logout
- Roles
- Claims
- Password Hash

---

## Auditoria

Implementar:

AuditInterceptor

AuditService

AuditRepository

---

## Logs

Registrar:

- Erros
- Exclusões
- Edições
- Login
- Importações

---

## Excel

Criar:

ExcelImporter

ExcelExporter

ExcelValidator

ExcelReader

ExcelWriter

---

## Storage

Preparar para:

Arquivos

Uploads

Relatórios

Exportações

---

## Dependências Permitidas

Domain

Application

Shared

---

## Dependências Proibidas

Web

Razor Pages

Views

---

## Checklist

☐ Configuração correta

☐ Logging aplicado

☐ Auditoria aplicada

☐ Identity aplicada

☐ Repository implementado

☐ Fluent API aplicada
