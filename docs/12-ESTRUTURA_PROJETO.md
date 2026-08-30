# Estrutura do Projeto

## Objetivo

Definir a organização física da solução, convenções, responsabilidades das camadas e padrões de desenvolvimento do Sistema Escolar.

Este documento é referência obrigatória para todos os desenvolvedores, agentes de IA, revisores e arquitetos envolvidos no projeto.

---

# Estrutura da Solution

```text
SistemaEscolar.sln

src/
│
├── SistemaEscolar.Web
├── SistemaEscolar.Application
├── SistemaEscolar.Domain
├── SistemaEscolar.Infrastructure
└── SistemaEscolar.Shared

tests/
│
├── SistemaEscolar.UnitTests
├── SistemaEscolar.IntegrationTests
└── SistemaEscolar.FunctionalTests

docs/

.escola-agent/
```

---

# Organização dos Projetos

## SistemaEscolar.Web

Responsável pela apresentação.

Contém:

- Razor Pages
- Layouts
- Componentes
- Filtros
- Middleware
- Configurações

---

## SistemaEscolar.Application

Responsável pelos casos de uso.

Contém:

- Services
- DTOs
- Validators
- Results
- Interfaces
- Commands
- Queries

---

## SistemaEscolar.Domain

Responsável pelas regras de negócio.

Contém:

- Entities
- Enums
- ValueObjects
- Specifications
- Events

---

## SistemaEscolar.Infrastructure

Responsável pela persistência.

Contém:

- DbContext
- Repositories
- Identity
- Audit
- Excel
- Storage

---

## SistemaEscolar.Shared

Responsável por recursos reutilizáveis.

Contém:

- Extensions
- Helpers
- Constants
- Exceptions
- Utilities

---

# Estrutura da Web

```text
SistemaEscolar.Web

Pages
Components
TagHelpers
Filters
Middleware
Configurations
Extensions
wwwroot
Program.cs
appsettings.json
```

---

# Estrutura das Razor Pages

As páginas deverão ser organizadas por domínio.

---

## Correto

```text
Pages

Dashboard

Usuarios

Professores

Alunos

Turmas

Disciplinas

Periodos

Notas

Importacoes

Relatorios

Auditoria

Configuracoes
```

---

## Incorreto

```text
Pages

Cadastros

Consultas

Diversos
```

---

# Estrutura de Módulo

Exemplo:

```text
Pages

Alunos

├── Index.cshtml
├── Index.cshtml.cs
├── Create.cshtml
├── Create.cshtml.cs
├── Edit.cshtml
├── Edit.cshtml.cs
├── Details.cshtml
├── Details.cshtml.cs
├── Delete.cshtml
└── Delete.cshtml.cs
```

---

# Shared Pages

```text
Pages

Shared

├── _Layout.cshtml
├── _ValidationScriptsPartial.cshtml
├── _Toast.cshtml
├── _Loading.cshtml
├── _Breadcrumb.cshtml
├── _PageHeader.cshtml
├── _EmptyState.cshtml
├── _ConfirmModal.cshtml
└── _DeleteModal.cshtml
```

---

# Components

```text
Components

Breadcrumb

PageHeader

Toast

EmptyState

Loading

Pagination

SearchPanel

DataTable

ConfirmModal

DeleteModal
```

---

# Estrutura da Application

```text
Application

Interfaces

Services

DTOs

Validators

Results

Mappings

Commands

Queries

Specifications
```

---

# Estrutura dos DTOs

## Correto

```text
CreateAlunoDto

UpdateAlunoDto

AlunoDto

AlunoFiltroDto

ImportAlunoDto
```

---

## Incorreto

```text
AlunoModel

AlunoData

DadosAluno
```

---

# Estrutura dos Services

```text
AlunoService

ProfessorService

TurmaService

DisciplinaService

NotaService

ImportacaoService

DashboardService

RelatorioService
```

---

# Result Pattern

Todos os serviços devem retornar:

```csharp
Result

Result<T>
```

---

## Exemplo

```csharp
Result<AlunoDto>
```

---

# Estrutura do Domain

```text
Domain

Entities

Enums

Specifications

Interfaces

Events

ValueObjects
```

---

# Entidades

```text
Usuario

Perfil

Professor

Aluno

Turma

Disciplina

PeriodoLancamento

Nota

AuditLog
```

---

# Estrutura da Infrastructure

```text
Infrastructure

Persistence

Repositories

Identity

Audit

Excel

Storage

Logging
```

---

# Repositories

```text
IAlunoRepository

AlunoRepository

INotaRepository

NotaRepository

IProfessorRepository

ProfessorRepository
```

---

# DbContext

```text
ApplicationDbContext
```

---

# Configurações EF Core

```text
Configurations

AlunoConfiguration

ProfessorConfiguration

TurmaConfiguration

DisciplinaConfiguration

NotaConfiguration
```

---

# Auditoria

```text
Audit

AuditInterceptor

AuditService

AuditRepository

AuditLog
```

---

# Importação

```text
Excel

ExcelImporter

ExcelReader

ExcelWriter

ExcelValidator

ImportHistory
```

---

# Estrutura do Shared

```text
Shared

Constants

Extensions

Exceptions

Helpers

Utilities

Pagination

Results
```

---

# ViewModels

Nunca reutilizar ViewModels genéricos.

---

## Correto

```text
AlunoListViewModel

AlunoCreateViewModel

AlunoEditViewModel

AlunoDetailsViewModel
```

---

## Incorreto

```text
AlunoViewModel
```

---

# Organização do wwwroot

```text
wwwroot

css

js

images

fonts

vendors
```

---

# CSS

```text
site.css

layout.css

components.css

forms.css

tables.css

dashboard.css

responsive.css
```

---

# JavaScript

```text
site.js

datatable.js

forms.js

upload.js

audit.js

dashboard.js
```

---

# Bibliotecas Front-End

## Permitidas

```text
Bootstrap

Bootstrap Icons

SweetAlert2

TomSelect

DataTables
```

---

## Não Permitidas

Adicionar frameworks sem aprovação arquitetural.

---

# Estrutura de Testes

```text
tests

SistemaEscolar.UnitTests

SistemaEscolar.IntegrationTests

SistemaEscolar.FunctionalTests
```

---

# Unit Tests

Testar:

```text
Services

Validators

Specifications

Domain
```

---

# Integration Tests

Testar:

```text
Repositories

DbContext

Identity

Importação
```

---

# Functional Tests

Testar:

```text
Fluxos completos

Login

Notas

Importação

Relatórios
```

---

# Convenções de Nome

## Classes

```text
PascalCase
```

Exemplo:

```csharp
AlunoService
```

---

## Métodos

```text
PascalCase
```

Exemplo:

```csharp
CadastrarAluno()
```

---

## Variáveis

```text
camelCase
```

Exemplo:

```csharp
alunoDto
```

---

## Constantes

```text
UPPER_CASE
```

Exemplo:

```csharp
MAX_IMPORT_SIZE
```

---

# Fluxo Arquitetural

Fluxo obrigatório:

```text
Usuário
     ↓
Page
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
```

---

# Fluxo de Retorno

```text
SQLite
     ↓
Repository
     ↓
Service
     ↓
Result
     ↓
PageModel
     ↓
Usuário
```

---

# Proibições

Nunca permitir:

❌ DbContext na UI

❌ SQL em Razor Pages

❌ Regra de negócio em Repository

❌ HTML duplicado

❌ JavaScript duplicado

❌ Dependência circular

❌ Service acessando diretamente Razor Pages

---

# Checklist Arquitetural

☐ Solution criada

☐ Projetos criados

☐ Estrutura respeitada

☐ DTOs padronizados

☐ ViewModels especializados

☐ Repositories padronizados

☐ Result Pattern implementado

☐ Components reutilizáveis criados

☐ Shared estruturado

☐ Testes organizados

☐ Convenções respeitadas

☐ Fluxo arquitetural respeitado

☐ Sem dependências circulares

☐ Sem duplicação de código
