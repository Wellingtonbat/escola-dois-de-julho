# AGENT: SOFTWARE ARCHITECT

## Missão

Definir padrões de implementação do software.

Responsável pela estrutura interna da aplicação.

---

## Stack Oficial

ASP.NET Core Razor Pages

.NET 9

Entity Framework Core

SQLite

Bootstrap

SweetAlert2

DataTables

TomSelect

Bootstrap Icons

---

## Arquitetura Interna

Fluxo obrigatório:

Page

↓

PageModel

↓

Application Service

↓

Repository

↓

DbContext

↓

SQLite

---

## Result Pattern

Todos os Services devem retornar:

Result

ou

Result<T>

Nunca:

bool

string

dynamic

object

---

## Estrutura de Services

Correto:

AlunoService

NotaService

UsuarioService

PeriodoLancamentoService

---

## Estrutura de DTOs

CreateAlunoDto

UpdateAlunoDto

AlunoDto

AlunoFiltroDto

ImportAlunoDto

---

## Estrutura de ViewModels

AlunoListViewModel

AlunoCreateViewModel

AlunoEditViewModel

AlunoDetailsViewModel

---

## Estrutura de Repositories

IAlunoRepository

AlunoRepository

INotaRepository

NotaRepository

---

## Validação

Obrigatória:

- Entrada
- Regras de negócio
- Permissões

---

## Tratamento de Erros

Obrigatório:

- Logging
- Mensagens amigáveis
- Result Pattern

---

## Regras de Aprovação

Nenhum código é aprovado quando:

- Existe duplicação
- Existe lógica em Razor
- Existe lógica em Repository
- Existe acesso direto ao DbContext
- Existe dependência indevida
