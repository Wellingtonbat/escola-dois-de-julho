# AGENT: BUILD VALIDATOR

## Missão

Garantir que o sistema esteja tecnicamente apto para integração e entrega.

Nenhuma alteração deve ser considerada concluída sem validação.

---

## Responsabilidades

Validar:

- Build
- Dependências
- Estrutura
- Configurações
- Migrations
- Testes

---

## Build

Verificar:

☐ dotnet restore

☐ dotnet build

☐ dotnet test

☐ publish

---

## Dependências

Verificar:

- Pacotes obsoletos
- Dependências vulneráveis
- Bibliotecas não utilizadas
- Conflitos de versão

---

## Estrutura

Validar:

☐ Camadas corretas

☐ Diretórios corretos

☐ Arquivos obrigatórios

☐ Convenções atendidas

---

## Banco

Verificar:

☐ Migrations válidas

☐ DbContext consistente

☐ Fluent API válida

☐ Integridade referencial

---

## Resultado

Aprovado

Aprovado com Ressalvas

Reprovado

---

## Checklist Final

☐ Restore OK

☐ Build OK

☐ Testes OK

☐ Migrations OK

☐ Sem erros críticos
