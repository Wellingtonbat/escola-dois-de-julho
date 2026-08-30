# Sistema Escolar

Sistema de gestão acadêmica para escolas: cadastro de alunos, professores, turmas, séries e disciplinas, lançamento de notas por período, boletim de resultados e controle de acesso por perfil (Diretor, Coordenador, Secretária e Professor).

## Stack

- **.NET 10** / ASP.NET Core Razor Pages
- **Entity Framework Core 9** + **PostgreSQL** (hospedado no [Neon](https://neon.tech))
- **ASP.NET Core Identity** para autenticação e perfis de acesso

## Estrutura do projeto

```
src/
  SistemaEscolar.Domain          Entidades e regras de domínio
  SistemaEscolar.Application     Casos de uso, DTOs e interfaces de serviço
  SistemaEscolar.Infrastructure  EF Core, repositórios, Identity, migrations
  SistemaEscolar.Web             Razor Pages (UI)
tests/
  SistemaEscolar.Tests           Testes automatizados
docs/                            Documentação funcional e técnica
```

## Configuração local

1. Restaure os pacotes e compile:
   ```
   dotnet build SistemaEscolar.sln
   ```

2. Configure a connection string do banco (Neon/Postgres) via **user-secrets** — nunca em `appsettings.json`:
   ```
   cd src/SistemaEscolar.Web
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<sua-connection-string>"
   ```

3. Copie `src/SistemaEscolar.Web/appsettings.Development.json.example` para `appsettings.Development.json` e ajuste o CPF/senha do usuário Diretor que será criado no primeiro seed (esse arquivo não é versionado, pois contém uma senha).

4. Aplique as migrations:
   ```
   cd src/SistemaEscolar.Web
   dotnet ef database update --project ../SistemaEscolar.Infrastructure --startup-project .
   ```

5. Rode a aplicação:
   ```
   dotnet run --project src/SistemaEscolar.Web
   ```

## Testes

```
dotnet test SistemaEscolar.sln
```
