# Comunicação Entre Agentes

## Objetivo

Definir como os agentes colaboram.

---

## Fluxo Padrão

Orchestrator

↓

Architect

↓

Developer

↓

Reviewer

↓

Technical Writer

---

## Regra Fundamental

Nenhum agente modifica decisões arquiteturais sozinho.

Toda alteração deve ser enviada para:

Solution Architect

---

## Exemplo

Nova funcionalidade:

Cadastro de Recuperação.

Fluxo:

Orchestrator
↓
Solution Architect
↓
Database Architect
↓
Application Developer
↓
Infrastructure Developer
↓
Razor Pages Developer
↓
Code Reviewer
↓
Technical Writer

---

## Conflitos

Quando dois agentes discordarem:

Prevalece:

1. Solution Architect
2. Security Architect
3. Database Architect
4. Developer

---

## Escalonamento

Mudança estrutural:

Obrigatório envolver:

- Solution Architect
- Software Architect

Mudança de banco:

Obrigatório envolver:

- Database Architect
- EF Core
