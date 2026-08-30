# Autenticação e Autorização

## Objetivo

Definir a estratégia de autenticação e autorização do Sistema Escolar.

Este documento estabelece:

- Login
- Logout
- Controle de sessões
- Roles
- Claims
- Políticas de autorização
- Recuperação de senha
- Segurança da aplicação

---

# Tecnologia

Autenticação:

ASP.NET Core Identity

Autorização:

ASP.NET Core Authorization

Controle de Sessão:

Cookies Authentication

Banco:

SQLite

Persistência:

Entity Framework Core

---

# Objetivos da Segurança

Garantir:

- Identificação do usuário
- Controle de acesso
- Proteção dos dados
- Rastreabilidade
- Auditoria

---

# Fluxo de Autenticação

```text
Usuário

↓

Tela Login

↓

Validação Credenciais

↓

Identity

↓

Cookie Autenticado

↓

Sistema
```

---

# Fluxo de Logout

```text
Usuário

↓

Logout

↓

Cookie Removido

↓

Sessão Encerrada

↓

Tela Login
```

---

# Usuário Autenticado

Após autenticação o sistema deverá conhecer:

```text
UserId

Nome

Email

Perfil

Claims

Permissões
```

---

# Estrutura de Identity

## Usuario

Utilizar customização do IdentityUser.

Exemplo:

```csharp
ApplicationUser
```

Campos adicionais:

```text
Nome

Ativo

UltimoLogin

DataCriacao
```

---

# Perfis Oficiais

## Diretor

Acesso administrativo.

---

## Professor

Acesso acadêmico.

---

# Roles

## Diretor

Permissão total.

---

## Professor

Permissão restrita.

---

# Claims

## Básicas

```text
UserId

UserName

Email

Role
```

---

## Acadêmicas

```text
ProfessorId

Turmas

Disciplinas
```

---

# Login

## Campos

```text
Usuário

Senha
```

---

## Validações

Usuário obrigatório.

Senha obrigatória.

Usuário deve estar ativo.

---

## Resultado

Autenticado.

ou

Negado.

---

# Logout

Ao realizar logout:

```text
Encerrar Sessão

Remover Cookie

Registrar Auditoria
```

---

# Políticas de Senha

## Tamanho mínimo

8 caracteres.

---

## Requisitos

Pelo menos:

```text
1 letra maiúscula

1 letra minúscula

1 número
```

---

## Futuro

Permitir configuração via painel administrativo.

---

# Troca de Senha

## Regras

Usuário autenticado.

Senha atual obrigatória.

Nova senha válida.

---

## Auditoria

Registrar:

```text
Usuário

Data

Hora
```

---

# Reset de Senha

## Responsável

Diretor.

---

## Fluxo

Selecionar usuário.

↓

Gerar senha temporária.

↓

Notificar usuário.

↓

Exigir troca no primeiro acesso.

---

# Primeiro Acesso

Usuário criado:

```text
Senha Temporária

↓

Login

↓

Troca Obrigatória

↓

Acesso Liberado
```

---

# Bloqueio de Conta

## Tentativas Inválidas

Quantidade:

5 tentativas consecutivas.

---

## Resultado

Usuário bloqueado.

---

## Desbloqueio

Somente Diretor.

---

# Sessão

## Duração

Inicial:

8 horas.

---

## Renovação

Renovar atividade válida.

---

## Expiração

Usuário deverá autenticar novamente.

---

# Cookies

## Configurações

HttpOnly

Secure

SameSite

---

## Objetivos

Proteção contra:

```text
XSS

Session Hijacking
```

---

# Proteção de Páginas

Todas as páginas protegidas.

Exemplo:

```csharp
[Authorize]
```

---

# Páginas Públicas

Somente:

```text
Login
```

---

# Páginas Restritas ao Diretor

```csharp
[Authorize(Roles="Diretor")]
```

Exemplos:

- Usuários
- Auditoria
- Configurações
- Importações
- Períodos

---

# Páginas Compartilhadas

```csharp
[Authorize(Roles="Diretor,Professor")]
```

Exemplos:

- Dashboard
- Notas
- Turmas
- Disciplinas

---

# Controle por Policy

## PolicyGerenciarUsuarios

```csharp
RequireRole("Diretor")
```

---

## PolicyImportacao

```csharp
RequireRole("Diretor")
```

---

## PolicyConsultarAuditoria

```csharp
RequireRole("Diretor")
```

---

## PolicyLancarNotas

```csharp
RequireRole("Diretor","Professor")
```

---

# Menus Dinâmicos

A visualização dos menus deverá respeitar as permissões.

---

## Menu Diretor

```text
Dashboard

Usuários

Professores

Alunos

Turmas

Disciplinas

Períodos

Notas

Importações

Relatórios

Auditoria

Configurações
```

---

## Menu Professor

```text
Dashboard

Alunos

Turmas

Disciplinas

Notas

Relatórios
```

---

# Autorização de Recursos

## Diretor

Pode visualizar todos os registros.

---

## Professor

Pode visualizar apenas:

- Turmas vinculadas
- Disciplinas vinculadas
- Alunos vinculados
- Notas vinculadas

---

# Auditoria de Segurança

Registrar:

```text
Login

Logout

Falha Login

Reset Senha

Troca Senha

Bloqueio Usuário

Desbloqueio Usuário

Alteração Perfil

Alteração Permissão
```

---

# Eventos Auditáveis

## Login

Registrar:

```text
Usuário

Data

Hora

IP
```

---

## Falha de Login

Registrar:

```text
Usuário

Data

Hora

IP
```

---

## Logout

Registrar:

```text
Usuário

Data

Hora
```

---

# Segurança de Uploads

Antes de processar arquivos:

Validar:

```text
Extensão

MimeType

Tamanho
```

---

# Regras Futuras

A arquitetura deverá suportar:

```text
MFA

Autenticação Microsoft

Autenticação Google

SSO

Azure AD
```

Sem alterações estruturais.

---

# Checklist de Segurança

☐ ASP.NET Core Identity configurado

☐ Roles configuradas

☐ Claims configuradas

☐ Policies configuradas

☐ Cookies seguros

☐ Login implementado

☐ Logout implementado

☐ Troca de senha implementada

☐ Reset de senha implementado

☐ Auditoria ativa

☐ Menus dinâmicos

☐ Autorização em todas as páginas

☐ Controle por perfil implementado

☐ Bloqueio por tentativas inválidas

☐ Estrutura preparada para MFA

☐ Estrutura preparada para SSO
