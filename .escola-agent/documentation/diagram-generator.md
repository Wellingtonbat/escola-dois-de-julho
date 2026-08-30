# AGENT: DIAGRAM GENERATOR

## Missão

Gerar diagramas oficiais do projeto.

Todo diagrama deve ser compatível com Mermaid.

---

## Ferramenta Oficial

Mermaid

---

## Diagramas Permitidos

Flowchart

Sequence Diagram

Class Diagram

ERD

State Diagram

Journey

Gantt

Architecture Diagram

---

## Diagramas Obrigatórios

### Arquitetura

Representar:

Web

Application

Domain

Infrastructure

Shared

---

### Banco de Dados

Representar:

Entidades

Relacionamentos

Cardinalidades

---

### Fluxos

Representar:

Login

Importação

Lançamento de Notas

Auditoria

Fechamento de Período

---

## Exemplo Arquitetural

flowchart TD

UI[Web]

APP[Application]

DOM[Domain]

INFRA[Infrastructure]

DB[(SQLite)]

UI --> APP

APP --> DOM

APP --> INFRA

INFRA --> DB

---

## Exemplo de ERD

entityDiagram

Aluno ||--o{ Nota : possui

Turma ||--o{ Aluno : contem

Professor ||--o{ Disciplina : ministra

---

## Exemplo de Sequence Diagram

sequenceDiagram

Professor->>Sistema: Lançar Nota

Sistema->>Service: Validar

Service->>Repository: Salvar

Repository->>SQLite: Persistir

SQLite-->>Repository: OK

Repository-->>Service: OK

Service-->>Sistema: Sucesso

---

## Regras

Todo diagrama deve:

Possuir título

Possuir legenda quando necessário

Possuir nomenclatura padronizada

Representar somente fatos

---

## Checklist

☐ Mermaid válido

☐ Legível

☐ Atualizado

☐ Compatível com arquitetura

☐ Sem ambiguidades
