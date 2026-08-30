# Perfis e Permissões

## Objetivo

Definir os perfis de acesso, permissões, recursos protegidos e estratégias de autorização do Sistema Escolar.

Este documento servirá como base para:

- ASP.NET Core Identity
- Roles
- Claims
- Policies
- Menus Dinâmicos
- Controle de Acesso

---

# Modelo de Segurança

O sistema utilizará:

RBAC

(Role Based Access Control)

---

# Conceitos

## Usuário

Pessoa autenticada no sistema.

---

## Perfil

Define o conjunto de permissões do usuário.

---

## Permissão

Ação que o usuário pode executar.

---

## Recurso

Funcionalidade protegida.

Exemplos:

- Alunos
- Professores
- Notas
- Relatórios

---

# Perfis Oficiais

## Diretor

Possui acesso administrativo.

---

## Professor

Possui acesso acadêmico limitado.

---

# Permissões do Sistema

## Consultar

Permite visualizar dados.

---

## Criar

Permite incluir registros.

---

## Editar

Permite alterar registros.

---

## Excluir

Permite remover registros.

---

## Importar

Permite importar dados.

---

## Exportar

Permite exportar dados.

---

## Administrar

Permite controle completo.

---

# Recursos Protegidos

## Usuários

Gerenciamento de usuários.

---

## Professores

Cadastro de professores.

---

## Alunos

Cadastro de alunos.

---

## Turmas

Cadastro de turmas.

---

## Disciplinas

Cadastro de disciplinas.

---

## Notas

Lançamento e manutenção de notas.

---

## Períodos

Controle de períodos de lançamento.

---

## Relatórios

Emissão de relatórios.

---

## Auditoria

Consulta aos registros auditados.

---

## Configurações

Configurações do sistema.

---

# Matriz de Permissões

## Usuários

| Permissão | Diretor | Professor |
| --------- | ------- | --------- |
| Consultar | Sim     | Não       |
| Criar     | Sim     | Não       |
| Editar    | Sim     | Não       |
| Excluir   | Sim     | Não       |

---

## Professores

| Permissão | Diretor | Professor |
| --------- | ------- | --------- |
| Consultar | Sim     | Não       |
| Criar     | Sim     | Não       |
| Editar    | Sim     | Não       |
| Excluir   | Sim     | Não       |

---

## Alunos

| Permissão | Diretor | Professor |
| --------- | ------- | --------- |
| Consultar | Sim     | Sim       |
| Criar     | Sim     | Não       |
| Editar    | Sim     | Não       |
| Excluir   | Sim     | Não       |

---

## Turmas

| Permissão | Diretor | Professor |
| --------- | ------- | --------- |
