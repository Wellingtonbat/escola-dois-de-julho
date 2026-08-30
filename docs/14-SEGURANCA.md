# Segurança

## Objetivo

Definir as diretrizes de segurança do Sistema Escolar.

Este documento estabelece padrões para:

- Autenticação
- Autorização
- Auditoria
- Proteção de dados
- Uploads
- Sessões
- Logs
- Compliance

---

# Princípios de Segurança

O sistema deverá seguir:

- Menor Privilégio
- Defesa em Profundidade
- Segurança por Padrão
- Segurança por Projeto
- Auditoria Completa

---

# Autenticação

Tecnologia:

ASP.NET Core Identity

---

## Regras

✅ Todo usuário deve autenticar

✅ Usuários inativos não acessam

✅ Bloqueio após tentativas inválidas

✅ Logout obrigatório

✅ Senhas criptografadas

---

# Política de Senha

## Requisitos

Mínimo:

8 caracteres

---

Obrigatório:

- 1 letra maiúscula
- 1 letra minúscula
- 1 número

---

Proibido:

- senha igual ao login
- senha em texto puro

---

# Perfis

## Diretor

Acesso administrativo.

---

## Professor

Acesso acadêmico restrito.

---

# Controle de Acesso

Modelo:

RBAC

Role Based Access Control

---

## Verificações

Todas as páginas devem validar:

- Usuário autenticado
- Perfil
- Permissão

---

# Claims

Obrigatórias:

```text
UserId

UserName

Role

Email
```

---

# Policies

Exemplos:

```text
GerenciarUsuarios

ImportarDados

GerenciarNotas

ConsultarAuditoria
```

---

# Proteção contra Ataques

## SQL Injection

Utilizar:

Entity Framework Core

Queries parametrizadas

---

## XSS

Escapar conteúdo exibido.

Não renderizar HTML arbitrário.

---

## CSRF

Utilizar AntiForgery Token.

Obrigatório em todos os formulários.

---

## Session Hijacking

Utilizar:

Cookies seguros

HTTPS

HttpOnly

SameSite

---

# Uploads

Validar:

✅ Extensão

✅ MimeType

✅ Tamanho

✅ Nome Arquivo

---

## Formatos Permitidos

```text
.xlsx

.csv

.pdf
```

---

## Formatos Bloqueados

```text
.exe

.bat

.cmd

.ps1

.sh
```

---

# Segurança dos Arquivos

Uploads nunca serão armazenados dentro do projeto.

Estrutura:

```text
Uploads

Importacoes

Relatorios

Temp
```

---

# Logs de Segurança

Registrar:

- Login
- Logout
- Falha login
- Reset senha
- Alteração perfil
- Alteração permissão

---

# Auditoria

Registrar:

✅ Inclusões

✅ Alterações

✅ Exclusões

✅ Importações

✅ Exportações

✅ Alterações de notas

---

# Dados Sensíveis

Nunca armazenar:

- Senha em texto
- Tokens em texto
- Segredos no código

---

# Configurações Sensíveis

Devem ficar em:

```text
appsettings

Secrets

Environment Variables
```

---

# LGPD

O sistema deverá permitir:

- Identificação dos dados armazenados
- Correção de dados
- Exclusão lógica
- Auditoria das alterações

---

# Backup

Definir:

Backup diário

Backup sob demanda

Restauração controlada

---

# Checklist de Segurança

☐ Identity configurado

☐ Roles configuradas

☐ Claims configuradas

☐ Policies configuradas

☐ Senhas protegidas

☐ Cookies seguros

☐ Upload seguro

☐ Logs ativos

☐ Auditoria ativa

☐ Proteção XSS

☐ Proteção CSRF

☐ Proteção SQL Injection

☐ LGPD atendida

☐ Backup definido
