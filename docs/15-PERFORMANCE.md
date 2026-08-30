# Performance

## Objetivo

Definir as diretrizes de performance do Sistema Escolar.

O sistema deve permanecer rápido mesmo com o crescimento dos dados.

---

# Objetivos

Garantir:

- Baixo tempo de resposta
- Escalabilidade
- Eficiência
- Baixo consumo de recursos

---

# Metas

## Consultas

Tempo médio:

< 2 segundos

---

## Dashboards

Tempo médio:

< 3 segundos

---

## Login

Tempo médio:

< 1 segundo

---

## Importação

Até:

5.000 registros

---

# Banco de Dados

## Utilizar Índices

Obrigatórios para:

```text
Login

Email

Matricula

ProfessorId

TurmaId

DisciplinaId

AnoLetivo
```

---

## Evitar

```text
SELECT *

Consulta sem filtros

Carga completa de tabelas
```

---

# Entity Framework Core

## Leitura

Sempre que possível:

```csharp
AsNoTracking()
```

---

## Escrita

Tracking apenas quando necessário.

---

## Includes

Utilizar somente quando necessário.

---

## Projeções

Preferir:

```csharp
Select()
```

ao invés de carregar entidades completas.

---

# Paginação

Obrigatória para:

- Alunos
- Professores
- Turmas
- Disciplinas
- Notas
- Auditorias

---

## Classe Padrão

```csharp
PagedResult<T>
```

---

# Dashboards

Carregar somente:

Indicadores necessários.

---

## Evitar

Consultas pesadas.

---

# Front-End

## DataTables

Sempre:

✅ Paginação

✅ Busca

✅ Ordenação

✅ Estado vazio

---

# JavaScript

Evitar:

- código duplicado
- múltiplas bibliotecas para mesma função

---

# Importação

Processamento por lote.

---

## Exibir

Quantidade processada

Sucesso

Falhas

Tempo execução

---

# Relatórios

Utilizar:

Filtros

Paginação

Consultas específicas

---

## Exportação

Gerar apenas mediante solicitação.

---

# Auditoria

Auditoria não pode degradar o sistema.

Utilizar:

Interceptors do EF Core.

---

# Consultas Críticas

Monitorar:

```text
Notas

Relatórios

Dashboards

Importações
```

---

# Crescimento Esperado

Suportar:

```text
10.000+ alunos

100.000+ notas

1.000.000+ registros de auditoria
```

---

# Boas Práticas

✅ Paginação

✅ Índices

✅ DTOs

✅ Projeções

✅ AsNoTracking

✅ Soft Delete

✅ Componentização

✅ Lazy Loading (avaliar)

---

# Problemas Proibidos

❌ N+1 Queries

❌ Includes desnecessários

❌ Loops custosos

❌ Consultas sem índice

❌ Carregamento completo de tabelas

❌ Duplicação de consultas

---

# Monitoramento

Métricas:

- Tempo resposta
- Consumo memória
- Tempo consultas
- Quantidade erros

---

# Teste de Performance

Realizar antes da versão 1.0:

✅ Login

✅ Consulta Alunos

✅ Consulta Notas

✅ Dashboard Diretor

✅ Dashboard Professor

✅ Importação

✅ Relatórios

---

# Checklist de Performance

☐ Índices implementados

☐ Paginação implementada

☐ DTOs utilizados

☐ AsNoTracking aplicado

☐ Projeções utilizadas

☐ Dashboards otimizados

☐ Relatórios otimizados

☐ Importação otimizada

☐ Sem N+1

☐ Sem Includes excessivos

☐ Testes executados

☐ Monitoramento implantado
