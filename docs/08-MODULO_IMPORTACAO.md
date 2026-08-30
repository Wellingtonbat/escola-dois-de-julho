# Módulo de Importação

## Objetivo

Permitir a importação segura, controlada e auditável de dados acadêmicos através de arquivos Excel e CSV.

O módulo deverá ser capaz de:

- Validar arquivos
- Validar registros
- Detectar inconsistências
- Exibir pré-visualização
- Importar dados em lote
- Gerar relatório de erros
- Registrar auditoria

---

# Escopo Inicial

Nesta primeira versão, o sistema permitirá importar:

✅ Alunos

✅ Notas

---

# Escopo Futuro

Preparar arquitetura para:

- Professores
- Turmas
- Disciplinas
- Frequência
- Usuários

---

# Perfis com Permissão

## Diretor

Pode:

- Importar
- Reprocessar
- Consultar histórico
- Exportar erros

---

## Professor

Não possui permissão.

---

# Formatos Aceitos

## Excel

```text
.xlsx
```

---

## CSV

```text
.csv
```

---

# Arquitetura do Módulo

Estrutura prevista:

```text
Infrastructure

Excel

├── ExcelImporter
├── ExcelExporter
├── ExcelValidator
├── ExcelReader
├── ExcelWriter
├── ImportResult
├── ImportError
└── ImportHistory
```

---

# Fluxo de Importação

```text
Upload Arquivo
        ↓
Validação Arquivo
        ↓
Leitura Dados
        ↓
Validação Registros
        ↓
Pré-Visualização
        ↓
Confirmação Usuário
        ↓
Importação
        ↓
Resumo Final
        ↓
Auditoria
```

---

# Processo de Upload

## Regras

Validar:

- Extensão
- Tamanho
- Estrutura
- Colunas obrigatórias

---

## Arquivos Inválidos

Devem ser rejeitados antes da leitura.

---

# Limites Iniciais

## Tamanho Máximo

10 MB

---

## Quantidade Máxima

5.000 registros

---

## Configuração Futura

Parametrizável.

---

# Importação de Alunos

## Layout Obrigatório

### Colunas

```text
Matricula

Nome

DataNascimento

Turma

Serie

Status
```

---

## Exemplo

```text
12345

Maria Silva

10/05/2012

7A

7º Ano

Ativo
```

---

# Validações de Alunos

## Obrigatórias

Matrícula

Nome

Turma

---

## Regras

Matrícula única.

Não permitir duplicidade.

---

## Inconsistências

Exemplos:

```text
Matrícula vazia

Nome vazio

Turma inexistente

Data inválida
```

---

# Importação de Notas

## Layout Obrigatório

### Colunas

```text
Aluno

Matricula

Disciplina

Professor

Periodo

Nota

Observacao
```

---

## Exemplo

```text
Maria Silva

12345

Matemática

Carlos Souza

1º Bimestre

8,5

Bom desempenho
```

---

# Validações de Notas

## Obrigatórias

Aluno

Disciplina

Professor

Período

Nota

---

## Regras

Aluno deve existir.

Professor deve existir.

Disciplina deve existir.

Período deve existir.

---

## Faixa de Nota

Valor mínimo:

0

Valor máximo:

10

Configuração futura:

Parametrizável.

---

# Pré-Visualização

Antes de salvar:

Exibir:

- Total de registros
- Registros válidos
- Registros inválidos
- Lista de erros

---

## Objetivo

Evitar importações incorretas.

---

# Mensagens de Erro

Exemplo:

```text
Linha 15

Matrícula não encontrada.
```

---

```text
Linha 27

Disciplina inexistente.
```

---

```text
Linha 43

Nota acima do permitido.
```

---

# Política de Erros

Registros inválidos:

Não interrompem toda importação.

---

## Exemplo

Arquivo:

```text
100 linhas
```

Resultado:

```text
95 importadas

5 rejeitadas
```

---

# Resumo Final

Exibir:

```text
Total Processado

Total Importado

Total Rejeitado

Tempo Processamento
```

---

## Exemplo

```text
Processados: 1000

Importados: 980

Rejeitados: 20
```

---

# Histórico de Importação

Tabela:

ImportHistory

---

## Campos

```text
Id

Arquivo

Modulo

Usuario

DataImportacao

QuantidadeProcessada

QuantidadeImportada

QuantidadeRejeitada

Status
```

---

# Status da Importação

```text
Processando

Concluída

Concluída com Erros

Cancelada

Falhou
```

---

# Registro de Erros

Tabela:

ImportError

---

## Campos

```text
Id

ImportHistoryId

Linha

Campo

MensagemErro
```

---

# Auditoria

Toda importação deve gerar auditoria.

---

## Registrar

```text
Usuário

Data

Hora

Arquivo

Módulo

Quantidade Registros
```

---

# Auditoria de Segurança

Registrar:

```text
Importação executada

Importação cancelada

Importação falhou

Tentativa sem permissão
```

---

# Estratégia de Transação

A operação deverá ser transacional.

---

## Opção Inicial

Processamento em lote.

---

## Estratégia

```text
Importar Registros Válidos

Registrar Registros Inválidos

Gerar Resumo
```

---

# Estrutura de Serviços

## Application

```text
ImportacaoService
```

---

## Infrastructure

```text
ExcelImporter

ExcelValidator

ExcelReader

ExcelWriter
```

---

## DTOs

```text
ImportAlunoDto

ImportNotaDto

ImportResultDto

ImportErrorDto
```

---

# Telas Previstas

```text
Importações

├── Upload
├── Pré-Visualização
├── Histórico
└── Detalhes
```

---

# DataTables

Histórico deverá possuir:

✅ Pesquisa

✅ Paginação

✅ Ordenação

✅ Exportação

✅ Consulta por período

---

# Dashboard

Indicadores:

```text
Importações Hoje

Importações Mês

Registros Importados

Registros Rejeitados
```

---

# Requisitos de Performance

O sistema deverá:

- Validar sem travar interface
- Processar até 5.000 registros
- Exibir progresso quando aplicável

---

# Requisitos de Segurança

Validar:

✅ Extensão

✅ MIME Type

✅ Tamanho

✅ Permissão usuário

✅ Estrutura arquivo

---

# Requisitos Futuros

Preparar arquitetura para:

- Importação assíncrona
- Processamento em background
- Filas de processamento
- Importação de múltiplos arquivos
- Integração com sistemas externos

---

# Checklist

☐ Upload implementado

☐ Leitura Excel implementada

☐ Leitura CSV implementada

☐ Validação implementada

☐ Pré-visualização implementada

☐ Histórico implementado

☐ Auditoria implementada

☐ Tratamento de erros implementado

☐ Resumo implementado

☐ Permissões implementadas

☐ Performance validada

☐ Segurança validada
