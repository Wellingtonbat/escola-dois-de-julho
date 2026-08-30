# Dashboards

## Objetivo

Definir os dashboards do Sistema Escolar.

Os dashboards serão as páginas iniciais do sistema e deverão fornecer informações relevantes para cada perfil de usuário.

Os indicadores deverão ser:

- Simples
- Objetivos
- Atualizados
- Responsivos
- Rápidos

---

# Perfis com Dashboard

Nesta primeira versão:

✅ Diretor

✅ Professor

---

# Objetivos dos Dashboards

Permitir que o usuário visualize rapidamente:

- Situação acadêmica
- Pendências
- Indicadores
- Últimas atividades
- Informações críticas

---

# Diretrizes de UX

Os dashboards devem:

✅ Ser intuitivos

✅ Possuir leitura rápida

✅ Utilizar cards

✅ Utilizar gráficos quando agregarem valor

✅ Evitar excesso de informações

✅ Funcionar em dispositivos móveis

---

# Padrão Visual

Template:

Sneat

---

## Componentes Permitidos

Cards

Alertas

DataTables

Gráficos

Badges

Progress Bars

Listas

---

# Dashboard Diretor

## Objetivo

Apresentar uma visão global da escola.

---

# Cards Principais

## Total de Alunos

Exibir:

```text
Quantidade total de alunos ativos.
```

---

## Total de Professores

Exibir:

```text
Quantidade de professores cadastrados.
```

---

## Total de Turmas

Exibir:

```text
Quantidade total de turmas.
```

---

## Períodos Abertos

Exibir:

```text
Quantidade de períodos atualmente abertos.
```

---

## Notas Lançadas

Exibir:

```text
Quantidade total de notas registradas.
```

---

## Importações do Dia

Exibir:

```text
Quantidade de importações realizadas.
```

---

# Indicadores Acadêmicos

## Alunos Ativos

```text
Total de alunos ativos.
```

---

## Alunos Inativos

```text
Total de alunos inativos.
```

---

## Professores Ativos

```text
Total de professores ativos.
```

---

## Turmas Ativas

```text
Quantidade de turmas em atividade.
```

---

# Indicadores Operacionais

## Notas Pendentes

Exibir:

```text
Quantidade de alunos sem nota lançada.
```

---

## Turmas sem Lançamento

Exibir:

```text
Turmas que ainda não possuem notas registradas.
```

---

## Períodos Próximos do Encerramento

Exibir:

```text
Períodos que expiram nos próximos 7 dias.
```

---

# Gráficos Diretor

## Notas por Turma

Tipo:

```text
Bar Chart
```

---

## Evolução de Lançamentos

Tipo:

```text
Line Chart
```

Exibir:

```text
Lançamentos por dia.
```

---

## Distribuição de Alunos

Tipo:

```text
Pie Chart
```

Agrupar por:

```text
Turma

Série
```

---

# Últimas Atividades

Exibir:

```text
Últimas notas lançadas

Últimas importações

Últimas alterações
```

---

## Campos

```text
Data

Usuário

Operação

Descrição
```

---

# Alertas Diretor

## Períodos Encerrando

Exibir alerta.

---

## Importações Falhadas

Exibir alerta.

---

## Professores sem Vínculo

Exibir alerta.

---

## Alunos sem Turma

Exibir alerta.

---

# Dashboard Professor

## Objetivo

Mostrar apenas informações relacionadas ao professor autenticado.

---

# Cards Principais

## Minhas Turmas

Exibir:

```text
Quantidade de turmas vinculadas.
```

---

## Minhas Disciplinas

Exibir:

```text
Quantidade de disciplinas vinculadas.
```

---

## Notas Lançadas

Exibir:

```text
Quantidade de notas registradas.
```

---

## Notas Pendentes

Exibir:

```text
Quantidade de lançamentos pendentes.
```

---

## Períodos Disponíveis

Exibir:

```text
Períodos atualmente abertos.
```

---

# Indicadores Professor

## Turmas Ativas

Total de turmas vinculadas.

---

## Disciplinas Ativas

Total de disciplinas vinculadas.

---

## Últimos Lançamentos

Total de lançamentos recentes.

---

# Gráficos Professor

## Lançamentos por Turma

Tipo:

```text
Bar Chart
```

---

## Evolução de Lançamentos

Tipo:

```text
Line Chart
```

---

## Distribuição de Notas

Tipo:

```text
Pie Chart
```

---

# Minhas Pendências

Exibir:

```text
Turmas sem lançamento

Disciplinas pendentes

Períodos próximos do vencimento
```

---

# Notificações

Professor visualizará:

```text
Períodos abertos

Períodos encerrando

Comunicados da direção
```

---

# Últimas Atividades

Exibir:

```text
Notas lançadas

Notas alteradas

Importações relacionadas
```

---

# Dashboard Responsivo

## Desktop

Exibir:

```text
4 a 6 cards por linha
```

---

## Tablet

Exibir:

```text
2 a 3 cards por linha
```

---

## Celular

Exibir:

```text
1 card por linha
```

---

# Performance

Regras:

✅ Carregar apenas dados necessários

✅ Utilizar consultas otimizadas

✅ Utilizar projeções

✅ Evitar Includes excessivos

✅ Utilizar cache futuro quando necessário

---

# Componentes Reutilizáveis

Criar:

```text
DashboardCard

StatisticCard

ActivityList

AlertCard

ChartWidget

SummaryCard
```

---

# Estrutura das Pages

```text
Pages

Dashboard

├── Diretor
│   ├── Index.cshtml
│   └── Index.cshtml.cs
│
└── Professor
    ├── Index.cshtml
    └── Index.cshtml.cs
```

---

# Serviços

Criar:

```text
DashboardService

ProfessorDashboardService

DiretorDashboardService
```

---

# DTOs

Criar:

```text
DashboardDto

DiretorDashboardDto

ProfessorDashboardDto

DashboardCardDto

DashboardChartDto
```

---

# Auditoria

Registrar:

```text
Acesso ao dashboard

Exportações

Consultas críticas
```

---

# Evoluções Futuras

Preparar estrutura para:

✅ Dashboard Coordenador

✅ Dashboard Secretaria

✅ Dashboard Aluno

✅ Dashboard Responsável

✅ Dashboard Mobile

---

# Checklist

☐ Dashboard Diretor implementado

☐ Dashboard Professor implementado

☐ Cards implementados

☐ Gráficos implementados

☐ Indicadores implementados

☐ Alertas implementados

☐ Últimas atividades implementadas

☐ Responsividade validada

☐ Performance validada

☐ Auditoria implementada

☐ Componentização aplicada

☐ Layout Sneat respeitado
