# Perfis e Permissões

## Como funciona hoje (a partir da v1.1.0)

As permissões não ficam mais fixas no código por nome de perfil. Cada perfil (role do ASP.NET Core Identity)
tem a sua lista de permissões gravada em `AspNetRoleClaims` (claim `sistemaescolar:permissao`), e a escola
edita essa lista na tela **Administração > Perfis e Permissões** (`/Perfis`), sem deploy.

- **Catálogo**: `SistemaEscolar.Application/Abstractions/Permissoes.cs` lista todas as permissões (código, módulo,
  nome e descrição) e as permissões padrão de cada perfil. Uma permissão nova só existe quando o código passa a
  verificá-la; por isso o catálogo é fixo e a tela apenas combina essas permissões em perfis.
- **Regras**: `PermissoesPerfil` continua sendo a fonte única das regras, agora em termos de permissão
  (`temPermissao`). Páginas usam `User.TemPermissao(...)`/extensões e serviços usam `ICurrentUserService.TemPermissao`.
- **Login**: as permissões dos perfis do usuário entram no cookie como claims (o `UserClaimsPrincipalFactory` do
  Identity já inclui os claims dos perfis). A revalidação do cookie a cada 1 minuto recarrega esses claims, então
  uma mudança de permissão chega a quem está logado em até 1 minuto, sem novo login.
- **Carga inicial**: na primeira inicialização após a atualização, `ApplicationDbInitializer` grava as permissões
  padrão (iguais às regras anteriores) em cada perfil e marca o perfil como inicializado; depois disso vale o que
  for salvo na tela. O perfil **Diretor** recebe sempre todas as permissões.
- **Visão restrita**: sem a permissão "Ver a escola inteira" (`Escola.VerTudo`), o usuário só enxerga as turmas e
  disciplinas em que é professor e não vê os Cadastros (é o caso do perfil Professor).
- **Anti-escalonamento** (`RegrasPerfis` e `PermissoesPerfil.CobrePermissoes`): só se concede uma permissão que se
  tem; só se altera/exclui um perfil, ou se gerencia uma conta, cujo perfil não tenha mais permissões que as suas;
  o Diretor não é editável; ninguém retira "Gerenciar perfis e permissões" do próprio perfil; perfis do sistema
  não são renomeados nem excluídos; perfil criado só é excluído sem usuários.
- **Auditoria**: alterações em `AspNetRoles` (perfis) e `AspNetRoleClaims` (permissões: Concedida/Retirada) são
  registradas; a carga inicial feita pelo sistema não.

O restante deste documento é o planejamento original e foi mantido como histórico.

---

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
