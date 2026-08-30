# Relatórios

## Objetivo

Definir os relatórios do Sistema Escolar.

Os relatórios deverão fornecer informações acadêmicas e gerenciais para apoio à tomada de decisão, acompanhamento pedagógico e emissão de documentos oficiais.

---

# Objetivos dos Relatórios

Permitir:

- Consulta de informações acadêmicas
- Acompanhamento de desempenho
- Emissão de boletins
- Controle de lançamentos
- Auditoria de alterações
- Exportação de dados

---

# Perfis com Acesso

## Diretor

Possui acesso a todos os relatórios.

---

## Professor

Possui acesso apenas aos relatórios relacionados:

- Às suas turmas
- Às suas disciplinas
- Aos seus lançamentos

---

# Padrão dos Relatórios

Todos os relatórios deverão possuir:

✅ Filtros

✅ Pesquisa

✅ Paginação

✅ Ordenação

✅ Exportação

✅ Responsividade

---

# Formatos de Exportação

## PDF

Para emissão formal.

---

## Excel

Para análises e consolidação.

---

## CSV

Para integração futura.

---

# Relatórios Acadêmicos

## Relatório de Boletim Escolar

### Objetivo

Exibir desempenho do aluno.

---

### Informações

```text
Aluno

Matrícula

Turma

Ano Letivo

Disciplinas

Notas

Médias

Situação Final
```

---

### Filtros

```text
Aluno

Turma

Ano Letivo

Período
```

---

### Exportação

✅ PDF

✅ Excel

---

# Relatório de Notas por Turma

### Objetivo

Exibir notas agrupadas por turma.

---

### Informações

```text
Turma

Aluno

Disciplina

Professor

Período

Nota
```

---

### Filtros

```text
Turma

Período

Disciplina

Professor
```

---

### Exportação

✅ PDF

✅ Excel

---

# Relatório de Notas por Professor

### Objetivo

Acompanhar lançamentos do professor.

---

### Informações

```text
Professor

Turma

Disciplina

Período

Quantidade de Lançamentos
```

---

### Filtros

```text
Professor

Turma

Disciplina

Período
```

---

### Exportação

✅ PDF

✅ Excel

---

# Relatório de Alunos Aprovados

### Objetivo

Listar alunos aprovados.

---

### Informações

```text
Aluno

Turma

Média Final

Situação
```

---

### Filtros

```text
Turma

Ano Letivo

Série
```

---

### Exportação

✅ PDF

✅ Excel

---

# Relatório de Alunos Reprovados

### Objetivo

Listar alunos reprovados.

---

### Informações

```text
Aluno

Turma

Média Final

Motivo
```

---

### Filtros

```text
Turma

Ano Letivo

Disciplina
```

---

### Exportação

✅ PDF

✅ Excel

---

# Relatório de Pendências de Lançamento

### Objetivo

Identificar lançamentos não realizados.

---

### Informações

```text
Professor

Turma

Disciplina

Período

Status
```

---

### Filtros

```text
Professor

Turma

Período
```

---

# Relatório de Períodos de Lançamento

### Objetivo

Consultar períodos cadastrados.

---

### Informações

```text
Descrição

Ano Letivo

Data Inicial

Data Final

Status
```

---

### Filtros

```text
Ano Letivo

Status
```

---

# Relatórios Gerenciais

## Relatório de Alunos

### Informações

```text
Matrícula

Nome

Turma

Status
```

---

### Filtros

```text
Nome

Matrícula

Turma

Status
```

---

# Relatório de Professores

### Informações

```text
Professor

E-mail

Disciplinas

Turmas

Status
```

---

### Filtros

```text
Professor

Disciplina

Turma

Status
```

---

# Relatório de Turmas

### Informações

```text
Nome

Série

Ano Letivo

Quantidade de Alunos
```

---

### Filtros

```text
Ano Letivo

Série

Turno
```

---

# Relatório de Disciplinas

### Informações

```text
Código

Nome

Carga Horária
```

---

### Filtros

```text
Código

Nome
```

---

# Relatórios de Auditoria

## Histórico de Alteração de Notas

### Objetivo

Rastrear alterações realizadas.

---

### Informações

```text
Aluno

Disciplina

Professor

Valor Anterior

Novo Valor

Usuário

Data
```

---

### Filtros

```text
Aluno

Professor

Usuário

Período
```

---

### Permissão

Somente Diretor.

---

# Histórico de Importações

### Informações

```text
Arquivo

Usuário

Data

Processados

Importados

Rejeitados
```

---

### Filtros

```text
Data

Usuário

Módulo
```

---

### Permissão

Somente Diretor.

---

# Dashboard Reports

Relatórios rápidos apresentados diretamente nos dashboards.

---

## Diretor

```text
Total Alunos

Total Professores

Períodos Abertos

Importações

Pendências
```

---

## Professor

```text
Turmas

Disciplinas

Notas Lançadas

Pendências
```

---

# Regras de Permissão

## Diretor

Pode acessar:

```text
Todos os relatórios
```

---

## Professor

Pode acessar apenas:

```text
Dados vinculados ao seu usuário
```

---

# Performance

## Boas Práticas

Utilizar:

✅ Paginação

✅ Projeções

✅ Consultas otimizadas

✅ AsNoTracking

✅ Índices

---

## Evitar

❌ SELECT \*

❌ Carregamento completo de tabelas

❌ Includes sem necessidade

---

# Estrutura de Telas

```text
Pages

Relatorios

├── Boletim
├── Alunos
├── Professores
├── Turmas
├── Disciplinas
├── Notas
├── Auditoria
└── Importacoes
```

---

# Estrutura de Serviços

```text
Application

RelatorioService

BoletimService

AuditoriaReportService

DashboardReportService
```

---

# DTOs

```text
BoletimDto

RelatorioAlunoDto

RelatorioProfessorDto

RelatorioTurmaDto

RelatorioNotaDto

RelatorioAuditoriaDto
```

---

# Componentes Compartilhados

Criar:

```text
ReportFilterComponent

ExportButtonComponent

ReportTableComponent

ReportSummaryComponent
```

---

# Auditoria

Registrar:

```text
Geração de relatório

Exportação PDF

Exportação Excel

Exportação CSV
```

---

# Evoluções Futuras

Preparar estrutura para:

✅ Histórico Escolar

✅ Frequência

✅ Recuperação

✅ Portal do Aluno

✅ Portal do Responsável

✅ BI Educacional

✅ Indicadores Avançados

---

# Checklist

☐ Boletim implementado

☐ Relatório de Alunos implementado

☐ Relatório de Professores implementado

☐ Relatório de Turmas implementado

☐ Relatório de Disciplinas implementado

☐ Relatório de Notas implementado

☐ Relatório de Auditoria implementado

☐ Exportação PDF implementada

☐ Exportação Excel implementada

☐ Permissões implementadas

☐ Auditoria implementada

☐ Performance validada

☐ Responsividade validada
