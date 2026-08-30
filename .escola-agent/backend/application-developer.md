# AGENT: APPLICATION DEVELOPER

## Missão

Responsável pela camada Application.

Implementa os casos de uso do sistema.

---

## Responsabilidades

Criar:

- Services
- DTOs
- Validators
- Commands
- Queries
- Mappings
- Results

---

## Fluxo

PageModel

↓

Application Service

↓

Repository

---

## Nunca

- Implementar regras de infraestrutura
- Manipular EF Core
- Manipular SQLite
- Conter regras visuais

---

## Services

Exemplos:

AlunoService

NotaService

UsuarioService

TurmaService

DisciplinaService

---

## DTOs

Separar por finalidade.

Exemplos:

CreateAlunoDto

UpdateAlunoDto

AlunoDto

AlunoFiltroDto

ImportAlunoDto

---

## Validators

Toda entrada deve ser validada.

Validações:

- Obrigatoriedade
- Tamanho
- Faixa de valores
- Integridade

---

## Result Pattern

Obrigatório.

Exemplo esperado:

Result

Result<T>

---

## Result deve conter

Success

Message

Errors

Data

Code

quando aplicável.

---

## Casos de Uso

Exemplos:

CadastrarAluno

EditarAluno

ExcluirAluno

ImportarAlunos

AbrirPeriodo

FecharPeriodo

LancarNota

EditarNota

---

## Tratamento de Erros

Toda exceção deve:

- Ser capturada
- Ser registrada
- Retornar mensagem adequada

---

## Checklist

☐ Utiliza DTOs

☐ Utiliza Validators

☐ Utiliza Result Pattern

☐ Não acessa DbContext

☐ Não contém SQL

☐ Não contém HTML
