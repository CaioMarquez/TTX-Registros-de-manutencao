# TTX Equipamentos - Sistema de Manutenção

## Visão Geral do Projeto

Este é um **sistema de gerenciamento de manutenção industrial**.

### Características Principais
- ✅ 13 páginas funcionais (Dashboard, Máquinas, Nova OS, Minhas OS, Histórico, Calendário, Indicadores, Custos Extras, Usuários, Backups, Perfil, Configurações de Alertas, Autenticação)
- ✅ Persistência de dados em JSON local (C:\ttx-dados\)
- ✅ Autenticação com conta mestre
- ✅ Controle de acesso baseado em roles (admin, supervisor, tecnico)
- ✅ Cálculos de disponibilidade de máquinas (70 máquinas, meta de 95%)
- ✅ Gerenciamento completo de ordens de serviço
- ✅ Interface Tailwind-inspired com XAML/WPF
- ✅ MVVM pattern para manutenibilidade

---

## Estrutura do Projeto

```
TTXEquipamentos/
├── Models/                      # Modelos de domínio (entidades)
│   └── DomainModels.cs         # User, Machine, MaintenanceRecord, etc.
│
├── Services/                    # Camada de lógica de negócio
│   ├── ServiceInterfaces.cs    # Contracts dos serviços
│   ├── ServiceImplementations.cs # Implementações
│   ├── NavigationServiceImpl.cs # Navegação entre páginas
│   └── JsonLocalDatabaseService.cs → Data/
│
├── Data/                        # Camada de persistência
│   └── JsonLocalDatabaseService.cs # CRUD em JSON
│
├── ViewModels/                  # MVVM ViewModels
│   ├── ViewModelBase.cs        # Base class com INotifyPropertyChanged
│   └── AuthAndDashboardViewModels.cs
│
├── Views/                       # XAML pages
│   ├── AppShell.xaml           # Shell da aplicação (sidebar + header)
│   └── Pages/
│       ├── Auth.xaml           # Login
│       ├── Dashboard.xaml      # Dashboard com KPIs
│       ├── Machines.xaml       # Gerenciamento de máquinas
│       ├── NewOS.xaml          # Criar nova ordem de serviço
│       ├── MyOS.xaml           # Minhas ordens
│       ├── History.xaml        # Histórico
│       ├── Calendar.xaml       # Calendário
│       ├── Indicators.xaml     # Analytics
│       ├── ExtraCosts.xaml     # Custos extras
│       ├── Users.xaml          # Gerenciamento de usuários
│       ├── Backups.xaml        # Backups
│       ├── Profile.xaml        # Perfil do usuário
│       └── AlertSettings.xaml  # Configurações de alertas
│
├── Resources/                   # XAML resources
│   ├── Colors.xaml             # Paleta Tailwind
│   ├── Styles.xaml             # Estilos globais
│   ├── ComponentStyles.xaml    # Estilos de componentes
│   └── Converters.xaml         # Value converters
│
├── Converters/
│   └── ValueConverters.cs      # BoolToVisibility, RoleToVisibility, etc.
│
├── Utilities/
│   └── Helpers.cs              # DateCalculationHelper, FileHelper, etc.
│
├── App.xaml & App.xaml.cs      # Aplicação principal
├── MainWindow.xaml & .cs       # Janela principal
├── TTXEquipamentos.csproj      # Arquivo de projeto
└── appSettings.json            # Configurações

Dados (Pasta C:\ttx-dados\):
├── profiles.json               # Usuários
├── user_roles.json             # Roles dos usuários
├── machines.json               # ~70 máquinas
├── maintenance_records.json    # Ordens de serviço
├── maintenance_items.json      # Itens de manutenção (partes, custos)
├── maintenance_plan.json       # Plano de manutenção
├── checklist_templates.json    # Templates de checklist
├── requesters.json             # 33 departamentos solicitantes
├── contractors.json            # Prestadores de serviço
├── extra_costs.json            # Custos extras
├── email_settings.json         # Configuração de email
└── system_diagnostics.json     # Logs e diagnósticos

```

---

## Primeiros Passos

### Pré-requisitos
- Windows 10+ com .NET 8.0 Runtime instalado
- Visual Studio 2022 (opcional, recomendado para desenvolvimento)

### Instalação & Execução

1. **Navegar até a pasta do projeto:**
   ```powershell
   cd "c:\Users\caio.moreira\3D Objects\ttxequipamentos-main - Copia\TTXEquipamentos"
   ```

2. **Restaurar NuGet packages:**
   ```powershell
   dotnet restore
   ```

3. **Compilar:**
   ```powershell
   dotnet build
   ```

4. **Executar:**
   ```powershell
   dotnet run
   ```

### Credenciais de Teste

**Master Admin Account:**
- Email: `suporte.master@ttx.com.br`
- Senha: `TTX@Master.2025!`

**Atalho de Master Admin:**
- Clique 5 vezes no ícone de chave inglesa (🔧) para auto-preencher as credenciais

---

## Funcionalidades Principais

### 1. **Autenticação**
- Login com email e senha
- Master admin com acesso total
- Roles: admin, supervisor, tecnico
- Salvamento de sessão

### 2. **Dashboard**
- KPI cards: Disponibilidade, Manutenção Preventiva, Corretiva, Total de Máquinas
- Atualização em tempo real
- Target de 95% de disponibilidade

### 3. **Gerenciamento de Máquinas**
- CRUD completo
- ~70 máquinas pré-cadastradas
- Filtro por área e tipo (elétrica/mecânica)

### 4. **Ordens de Serviço (OS)**
- Criar preventiva ou corretiva
- Atribuição a técnicos
- Checklist para manutenção preventiva
- Rastreamento de custos

### 5. **Histórico**
- Consultar todas as OSs
- Filtrar por data, máquina, status
- Editar registros
- Exportar para CSV/Excel (futuro)

### 6. **Indicadores**
- Disponibilidade mensal (%)
- Custos por período
- Gráficos de tendência
- Downtime em horas trabalhadas

### 7. **Controle de Acesso**
- Admin: Acesso total (usuários, backups, custos)
- Supervisor: Ver custos e manutenção
- Técnico: Criar/editar suas próprias OSs

---

## Fluxo de Dados

```
Usuário → MainWindow (Frame)
         ↓
      Auth.xaml (Login)
         ↓ (Sucesso)
      Dashboard.xaml (Sidebar + MainFrame)
         ↓
      [Seleciona página na sidebar]
         ↓
      Página selecionada (Frame navigation)
         ↓
      ViewModel → Services → LocalDatabaseService
         ↓
      JSON files (C:\ttx-dados\*.json)
```

---

## Estilo Visual (Tailwind-inspired)

### Paleta de Cores
- **Primary**: #3B82F6 (Azul)
- **Success**: #10B981 (Verde)
- **Warning**: #F59E0B (Âmbar)
- **Error**: #EF4444 (Vermelho)
- **Gray**: #1F2937 - #F9FAFB

### Componentes
- Buttons: Primary, Secondary, Danger
- Input fields: TextBox, PasswordBox
- Cards: Bordered, com padding
- Badges: Para status (ok/nao_ok/na)
- DataGrids: Com hover effects

---

## Cálculos de Disponibilidade

### Horário de Funcionamento
- **Seg-Qui**: 07:30-17:30 (10 horas/dia)
- **Sex**: 07:30-16:30 (9 horas/dia)
- **Sábado/Domingo**: Fechado

### Fórmula
```
Disponibilidade (%) = (Horas Agendadas - Horas Paradas) / Horas Agendadas × 100

Horas Agendadas = 70 máquinas × (horas/mês)
Horas Paradas = Soma de duração das OSs corretivas (em horas trabalho)
```

---

## Segurança

### Autenticação
- Armazenamento local de sessão
- Senha simples (em produção, usar hash bcrypt/Argon2)
- Master admin com acesso irrestrito

### Controle de Acesso
- Row-Level Security equivalente via filtering nos dados
- Menu sidebar responsivo a roles

---

## Dependências NuGet

- **Newtonsoft.Json** - Serialização JSON
- **CommunityToolkit.Mvvm** - Suporte MVVM
- **MaterialDesignThemes** - Design system
- **MaterialDesignColors** - Cores Material Design
- **System.ComponentModel.DataAnnotations** - Validação
- **OxyPlot.Wpf** - Gráficos (Charts)
- **Serilog** - Logging estruturado

---

## Extensões Futuras

### Curto prazo
- [ ] Implementar exportação para Excel/CSV na página History
- [ ] Gráficos interativos com OxyPlot/WpfPlot
- [ ] Autosave de rascunhos de OSs
- [ ] Notificações toast ao criar/editar registros

### Médio prazo
- [ ] Dark mode toggle
- [ ] Multi-idioma (EN, PT, ES)
- [ ] Auditoria de mudanças (quem editou, quando)
- [ ] Backup automático com versionamento
- [ ] Sincronização com banco de dados central (Supabase/SQL Server)

### Longo prazo
- [ ] Integração com email para alertas
- [ ] API REST para integração com sistemas terceiros
- [ ] Mobile app (MAUI)
- [ ] Painéis de controle em tempo real

---

## Testes

### Para testar a aplicação:

1. **Login com Master Admin:**
   - Use credenciais fornecidas
   - Verifique acesso a todas as páginas

2. **Criar Máquina (página Máquinas):**
   - Deve salvar em machines.json
   - Verificar se aparece em Dashboard

3. **Criar Ordem de Serviço:**
   - Tipo preventiva/corretiva
   - Atribuição a técnico
   - Verificar em Histórico

4. **Verificar Disponibilidade:**
   - Dashboard mostra % correto
   - Cálculos refletem OSs criadas

---

## Struktura de Dados JSON

### Exemplo: profiles.json
```json
[
  {
    "id": "user_1",
    "email": "suporte.master@ttx.com.br",
    "password": "TTX@Master.2025!",
    "name": "Master Admin",
    "created_at": "2026-05-12T00:00:00",
    "updated_at": "2026-05-12T00:00:00"
  }
]
```

### Exemplo: machines.json
```json
[
  {
    "id": "machine_1",
    "tag": "MQ-001",
    "name": "Máquina 1",
    "area": "Caldeiraria",
    "type": "Elétrica",
    "created_at": "2026-05-12T00:00:00",
    "updated_at": "2026-05-12T00:00:00"
  }
]
```

### Exemplo: maintenance_records.json
```json
[
  {
    "id": "os_1",
    "type": "preventiva",
    "nature": "mecanica",
    "machine_id": "machine_1",
    "machine_tag": "MQ-001",
    "technician_id": "user_2",
    "technician_name": "João Silva",
    "start_time": "2026-05-12T08:00:00",
    "end_time": "2026-05-12T12:00:00",
    "status": "concluida",
    "total_cost": 150.00,
    "created_at": "2026-05-12T00:00:00",
    "updated_at": "2026-05-12T00:00:00"
  }
]
```

---

## Troubleshooting

### Problema: "No .NET SDKs were found"
**Solução:** Instale .NET 8.0 SDK do https://dotnet.microsoft.com/download

### Problema: App não inicia
**Solução:** Limpe cache e reconstrua:
```powershell
dotnet clean
dotnet build
```

### Problema: Dados desaparecem ao fechar app
**Solução:** Verifique se C:\ttx-dados\ existe e tem permissões de escrita

### Problema: Login não funciona
**Solução:** 
- Verifique se profiles.json existe em C:\ttx-dados\
- Confirme email e senha corretos
- Tente o atalho do master admin (5 cliques no 🔧)

---

## Licença

Propriedade de TTX Equipamentos. Projeto interno.

---

## Autor

Caio Moreira - Desenvolvedor - TTX Equipamentos

---

**Última atualização:** 09 de julho de 2026
