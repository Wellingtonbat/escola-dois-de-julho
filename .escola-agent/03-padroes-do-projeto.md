# Padrões Oficiais

## Linguagem

C#

.NET 9

ASP.NET Core Razor Pages

---

## Banco

SQLite

Entity Framework Core

Fluent API obrigatório.

---

## UI

Sneat

Bootstrap 5

Bootstrap Icons

SweetAlert2

TomSelect

DataTables

---

## Convenções

Classes:

PascalCase

Métodos:

PascalCase

Variáveis locais:

camelCase

Constantes:

UPPER_CASE

---

## DTOs

Sempre específicos.

Correto:

AlunoDto

CreateAlunoDto

UpdateAlunoDto

ImportAlunoDto

Incorreto:

AlunoModel

AlunoData

AlunoGenerico

---

## ViewModels

Sempre especializados.

AlunoListViewModel

AlunoCreateViewModel

AlunoEditViewModel

AlunoDetailsViewModel

---

## Services

Devem retornar:

Result

Result<T>

Nunca retornar string.

Nunca retornar bool.
