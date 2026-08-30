# Sistema Escolar

> Sistema de Gestão Escolar desenvolvido em **ASP.NET Core Razor Pages**, **Entity Framework Core** e **SQLite**, utilizando o template administrativo **Sneat** como padrão visual.

---

# Objetivo

O Sistema Escolar tem como objetivo informatizar o processo de gerenciamento acadêmico da escola, permitindo que a direção acompanhe todas as atividades pedagógicas e que os professores realizem o lançamento das notas dos alunos de forma simples, segura e auditável.

O projeto foi concebido para ser altamente organizado, de fácil manutenção e preparado para futuras expansões.

O sistema deverá atender inicialmente apenas **uma escola**, porém sua arquitetura deverá permitir evolução para múltiplas unidades no futuro.

---

# Objetivos do Projeto

- Centralizar o cadastro dos alunos.
- Centralizar os lançamentos de notas.
- Permitir controle dos períodos de lançamento.
- Garantir rastreabilidade através de auditoria.
- Padronizar a interface utilizando o Sneat.
- Facilitar futuras implementações.
- Minimizar código duplicado.
- Criar uma arquitetura limpa e desacoplada.

---

# Tecnologias

## Backend

- ASP.NET Core Razor Pages (.NET 9)
- Entity Framework Core
- SQLite

## Frontend

- Bootstrap 5
- Sneat Admin Template
- Bootstrap Icons
- Tom Select
- SweetAlert2
- DataTables
- JavaScript ES6

---

# Arquitetura

O projeto seguirá uma arquitetura em camadas.

```
Pages
      ↓

Application Services
      ↓

Repositories
      ↓

Entity Framework
      ↓

SQLite
```

Nenhuma regra de negócio deverá existir nas Razor Pages.

Toda regra ficará concentrada na camada de Services.

---

# Estrutura do Projeto

```
SistemaEscolar

src/

    SistemaEscolar.Web

    SistemaEscolar.Application

    SistemaEscolar.Domain

    SistemaEscolar.Infrastructure

tests/

docs/

.escola-agent/
```

---

# Layout

Toda interface deverá utilizar como referência o projeto informado pelo usuário contendo o template Sneat.

O agente deverá analisar o projeto de referência antes de criar qualquer tela.

O objetivo é manter:

- mesmo padrão visual;
- mesmos componentes;
- mesma navegação;
- mesmas cores;
- mesma experiência do usuário.

---

# Banco de Dados

Banco utilizado:

SQLite

ORM:

Entity Framework Core

As migrations deverão ser criadas utilizando o EF Core.

Nenhuma alteração estrutural poderá ser realizada manualmente no banco.

---

# Perfis

Inicialmente existirão dois perfis.

## Diretor

Possui acesso total ao sistema.

Permissões:

- Gerenciar usuários
- Importar planilhas
- Cadastrar alunos
- Editar alunos
- Editar notas
- Abrir períodos
- Fechar períodos
- Consultar auditoria
- Emitir relatórios
- Dashboard administrativo

---

## Professor

Permissões:

- Visualizar suas turmas
- Complementar cadastro do aluno
- Lançar notas
- Editar notas enquanto o período estiver aberto
- Consultar histórico

---

# Login

O login será realizado utilizando:

CPF

As senhas deverão ser armazenadas utilizando hash.

Nunca armazenar senha em texto puro.

---

# Importação

O sistema possuirá importação inicial através de planilhas Excel.

Serão importados:

- Alunos
- Professores
- Turmas
- Disciplinas
- Notas

Toda importação deverá possuir validação.

---

# Auditoria

Todo processo que modificar o banco deverá ser auditado.

Registrar automaticamente:

- INSERT
- UPDATE
- DELETE

Além disso:

- Login
- Logout
- Alteração de senha
- Importações
- Exportações

A auditoria deverá ser automática através do Entity Framework Core.

---

# Dashboard

O sistema possuirá dashboards específicos.

## Diretor

Indicadores:

- Total de alunos
- Total de professores
- Turmas
- Períodos
- Notas pendentes
- Recuperações
- Últimas alterações

## Professor

Indicadores:

- Turmas
- Disciplinas
- Pendências
- Últimos lançamentos

---

# Layout das Telas

Todas as páginas deverão possuir:

- Breadcrumb
- Título
- Subtítulo
- Botões de ação
- Cards padronizados
- DataTables
- Pesquisa
- Paginação
- Mensagens amigáveis

---

# Padrões de Desenvolvimento

Seguir obrigatoriamente:

- SOLID
- Clean Code
- Repository Pattern
- Service Pattern
- Dependency Injection
- Async/Await
- DTOs
- ViewModels

---

# Objetivo da Documentação

A pasta `.escola-agent` contém toda a documentação necessária para que agentes de IA compreendam o projeto.

Todos os documentos devem ser considerados como fonte oficial de requisitos.

Nenhuma implementação deverá ser iniciada antes da leitura completa dessa documentação.

---

# Estrutura da Documentação

```
.escola-agent

README.md

CLAUDE.md

copilot-instructions.md

00-VISAO_GERAL.md

01-OBJETIVOS.md

02-ARQUITETURA.md

03-DOMINIO.md

04-BANCO_DADOS.md

05-REGRAS_NEGOCIO.md

06-PERFIS_PERMISSOES.md

07-AUTENTICACAO.md

08-IMPORTACAO.md

09-AUDITORIA.md

10-LAYOUT_SNEAT.md

...

```

---

# Fluxo de Desenvolvimento

Sempre seguir a seguinte sequência:

1. Ler toda documentação.

2. Compreender a funcionalidade.

3. Verificar componentes reutilizáveis.

4. Implementar Services.

5. Implementar Repositories.

6. Criar Razor Pages.

7. Criar validações.

8. Criar testes quando aplicável.

9. Atualizar documentação.

10. Atualizar backlog.

---

# Filosofia do Projeto

Este projeto prioriza:

- Organização.
- Clareza.
- Simplicidade.
- Reutilização.
- Escalabilidade.
- Segurança.
- Padronização.
- Excelente experiência do usuário.

Toda implementação deverá seguir rigorosamente estes princípios.
