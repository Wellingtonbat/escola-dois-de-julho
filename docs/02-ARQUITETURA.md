# Arquitetura do Sistema

## Arquitetura em Camadas

O sistema seguirá arquitetura em camadas.

---

# Estrutura da Solution

src/

SistemaEscolar.Web

SistemaEscolar.Application

SistemaEscolar.Domain

SistemaEscolar.Infrastructure

SistemaEscolar.Shared

---

# Camadas

## Web

Responsável pela interface.

Contém:

- Razor Pages
- Componentes
- Layouts
- Dashboard

---

## Application

Responsável pelos casos de uso.

Contém:

- Services
- DTOs
- Validators
- Results

---

## Domain

Responsável pelas regras de negócio.

Contém:

- Entities
- Enums
- Value Objects
- Specifications

---

## Infrastructure

Responsável pela persistência.

Contém:

- EF Core
- SQLite
- Repositories
- Auditoria
- Identity

---

## Shared

Código reutilizável.

Contém:

- Helpers
- Extensions
- Constants
- Exceptions

---

# Fluxo

Usuário

↓

Razor Page

↓

PageModel

↓

Service

↓

Repository

↓

DbContext

↓

SQLite

---

# Result Pattern

Todos os serviços devem retornar:

Result

ou

Result<T>

---

# Regras

Não permitir:

- DbContext na UI
- Regra de negócio em Repository
- SQL em Razor Pages
- Dependências circulares

---

# Auditoria

Utilizar Interceptors do EF Core.

---

# Segurança

- Login
- Roles
- Claims
- Permissões

---

# Front-End

- Sneat
- Bootstrap
- TomSelect
- DataTables
- SweetAlert2
