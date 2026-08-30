# Modelo Inicial de Dados

## Usuario

### Campos

- Id
- Nome
- Email
- Login
- SenhaHash
- PerfilId
- Ativo

---

## Perfil

### Campos

- Id
- Nome

### Valores

- Diretor
- Professor

---

## Professor

### Campos

- Id
- Nome
- Email
- UsuarioId

---

## Aluno

### Campos

- Id
- Matricula
- Nome
- DataNascimento
- TurmaId
- Status

---

## Turma

### Campos

- Id
- Nome
- Serie
- AnoLetivo
- Turno

---

## Disciplina

### Campos

- Id
- Nome
- Codigo
- CargaHoraria

---

## ProfessorDisciplina

### Campos

- Id
- ProfessorId
- DisciplinaId

---

## ProfessorTurma

### Campos

- Id
- ProfessorId
- TurmaId

---

## PeriodoLancamento

### Campos

- Id
- AnoLetivo
- Descricao
- DataInicial
- DataFinal
- Status

---

## Nota

### Campos

- Id
- AlunoId
- DisciplinaId
- ProfessorId
- PeriodoId
- Valor
- Observacao
- DataLancamento

---

## AuditoriaNota

### Campos

- Id
- NotaId
- UsuarioId
- ValorAnterior
- ValorNovo
- DataAlteracao

---

# Relacionamentos

Perfil

1:N

Usuario

---

Turma

1:N

Aluno

---

Professor

N:N

Disciplina

---

Professor

N:N

Turma

---

Aluno

1:N

Nota

---

Disciplina

1:N

Nota

---

PeriodoLancamento

1:N

Nota

---

# Entidade Base

Todas as entidades devem herdar:

EntityBase

Campos:

- Id
- CreatedAt
- UpdatedAt
- CreatedBy
- UpdatedBy
- IsDeleted
