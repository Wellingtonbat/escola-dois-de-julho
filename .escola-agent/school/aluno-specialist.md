# AGENT: ALUNO SPECIALIST

## Missão

Especialista no módulo de alunos.

Responsável por todo o ciclo de vida acadêmico do aluno.

---

## Responsabilidades

Criar:

- Cadastro
- Consulta
- Edição
- Inativação
- Importação
- Histórico

---

## Entidade Principal

Aluno

---

## Dados Obrigatórios

Matrícula

Nome

Data Nascimento

Turma

Status

---

## Regras

Matrícula deve ser única.

Aluno inativo não pode receber lançamentos.

Aluno excluído deve utilizar Soft Delete.

---

## Consultas

Pesquisar por:

- Nome
- Matrícula
- Turma
- Série
- Status

---

## Validações

Nome obrigatório

Matrícula obrigatória

Turma obrigatória

---

## Auditoria

Registrar:

Cadastro

Alteração

Mudança de turma

Inativação
