# Regras de Negócio

## Objetivo

Este documento define todas as regras de negócio do Sistema Escolar.

As regras aqui documentadas são obrigatórias e deverão ser implementadas na camada Domain e Application.

---

# Perfis do Sistema

## Diretor

Possui acesso completo ao sistema.

Pode:

- Gerenciar usuários
- Gerenciar professores
- Gerenciar alunos
- Gerenciar turmas
- Gerenciar disciplinas
- Abrir períodos
- Fechar períodos
- Alterar qualquer nota
- Consultar auditorias
- Executar importações
- Emitir relatórios

---

## Professor

Possui acesso acadêmico limitado.

Pode:

- Consultar suas turmas
- Consultar suas disciplinas
- Lançar notas
- Alterar notas próprias
- Consultar histórico de lançamentos

Não pode:

- Abrir períodos
- Fechar períodos
- Alterar notas de outros professores
- Alterar permissões

---

# Regras de Usuário

## RN001

Todo usuário deve possuir perfil.

---

## RN002

Login deve ser único.

---

## RN003

E-mail deve ser único.

---

## RN004

Usuários inativos não poderão autenticar.

---

## RN005

Toda alteração de perfil deve ser auditada.

---

# Regras de Professor

## RN006

Todo professor deve possuir usuário associado.

---

## RN007

Um professor pode estar vinculado a múltiplas turmas.

---

## RN008

Um professor pode estar vinculado a múltiplas disciplinas.

---

## RN009

Não permitir professores duplicados.

Validação:

Nome + E-mail

---

# Regras de Aluno

## RN010

Matrícula deve ser única.

---

## RN011

Todo aluno deve pertencer a uma turma.

---

## RN012

Aluno inativo não poderá receber lançamentos de notas.

---

## RN013

Aluno excluído deve utilizar Soft Delete.

---

## RN014

Mudança de turma deve ser auditada.

---

# Regras de Turma

## RN015

Toda turma deve possuir:

- Nome
- Série
- Ano Letivo

---

## RN016

Não permitir turmas duplicadas.

Validação:

Nome + Ano Letivo

---

## RN017

Aluno não pode estar matriculado duas vezes na mesma turma.

---

# Regras de Disciplina

## RN018

Toda disciplina deve possuir código único.

---

## RN019

Uma disciplina pode existir em várias turmas.

---

## RN020

Uma disciplina pode ser ministrada por mais de um professor.

---

# Regras de Período de Lançamento

## RN021

Somente diretores podem abrir períodos.

---

## RN022

Somente diretores podem fechar períodos.

---

## RN023

Não permitir períodos com datas inválidas.

---

## RN024

Não permitir períodos sobrepostos.

Exemplo:

Período A:

01/02/2026 até 31/03/2026

Período B:

15/03/2026 até 30/04/2026

Resultado:

Rejeitado

---

## RN025

Período fechado impede alterações de notas.

---

## RN026

Professor somente poderá lançar notas em períodos abertos.

---

## RN027

Diretores podem alterar notas mesmo após fechamento.

Toda alteração deve ser auditada.

---

# Regras de Notas

## RN028

Nota deve estar entre valor mínimo e máximo configurado.

Valor padrão inicial:

0 até 10

---

## RN029

Não permitir nota vazia.

---

## RN030

Toda nota deve estar associada a:

- Aluno
- Professor
- Disciplina
- Período

---

## RN031

Não permitir lançamento duplicado.

Critério:

Aluno + Disciplina + Período

---

## RN032

Professor somente poderá alterar notas lançadas por ele.

Exceção:

Diretor

---

## RN033

Toda alteração de nota deve gerar auditoria.

Registrar:

- Usuário
- Data
- Hora
- Valor anterior
- Novo valor

---

## RN034

Notas importadas devem possuir identificação de origem.

---

# Regras de Importação

## RN035

Somente usuários autorizados poderão importar dados.

Inicialmente:

Diretor

---

## RN036

Arquivos aceitos:

- XLSX
- CSV

---

## RN037

Arquivo deve ser validado antes da importação.

---

## RN038

Importação deve apresentar:

- Total processado
- Total importado
- Total rejeitado

---

## RN039

Linhas inválidas não devem interromper toda a importação.

---

## RN040

Todas as inconsistências devem ser registradas.

---

## RN041

Importações devem ser auditadas.

---

# Regras de Auditoria

## RN042

Toda alteração de nota deve ser auditada.

---

## RN043

Toda alteração de permissão deve ser auditada.

---

## RN044

Toda alteração de perfil deve ser auditada.

---

## RN045

Toda importação deve ser auditada.

---

## RN046

Toda exclusão lógica deve ser auditada.

---

## RN047

Registros de auditoria não poderão ser alterados.

---

# Regras de Relatórios

## RN048

Relatórios devem respeitar permissões.

---

## RN049

Professor somente visualiza dados vinculados.

---

## RN050

Diretor visualiza todos os dados.

---

## RN051

Relatórios deverão suportar filtros.

Exemplos:

- Ano Letivo
- Turma
- Professor
- Disciplina
- Período

---

# Regras de Dashboard

## RN052

Dashboard deve ser personalizado por perfil.

---

## RN053

Diretor visualiza indicadores globais.

---

## RN054

Professor visualiza apenas informações vinculadas.

---

# Regras de Segurança

## RN055

Todas as páginas requerem autenticação.

Exceção:

Login

---

## RN056

Todas as ações críticas devem validar permissão.

---

## RN057

Senhas nunca devem ser armazenadas em texto.

---

## RN058

Uploads devem validar:

- Extensão
- Tamanho
- Tipo

---

## RN059

Usuários sem permissão não devem visualizar menus restritos.

---

# Regras de Exclusão

## RN060

Exclusões serão lógicas.

Campo:

IsDeleted

---

## RN061

Registros excluídos não devem aparecer em consultas comuns.

---

## RN062

Exclusões devem permanecer auditáveis.

---

# Regras Futuras

Estas funcionalidades já devem ser consideradas na arquitetura.

## Recuperação Escolar

Permitir cálculo de recuperação.

---

## Frequência

Permitir controle de presença.

---

## Portal do Aluno

Permitir visualização de:

- Notas
- Boletins
- Frequência

---

## Portal do Professor

Permitir:

- Lançamento de notas
- Lançamento de frequência

---

# Checklist de Regras

☐ Todos os usuários possuem perfil

☐ Todos os alunos possuem matrícula única

☐ Todos os períodos são validados

☐ Todas as notas possuem vínculo acadêmico

☐ Todas as alterações são auditadas

☐ Toda importação é validada

☐ Relatórios respeitam permissões

☐ Exclusões utilizam Soft Delete

☐ Segurança aplicada em todas as funcionalidades

☐ Regras futuras consideradas na arquitetura
