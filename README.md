# Deathrun Manager Plugin

<div align="center">

![Version](https://img.shields.io/badge/version-0.2.0-blue.svg)
![CounterStrike Sharp](https://img.shields.io/badge/CounterStrike%20Sharp-compatible-green.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

*A comprehensive deathrun management plugin for Counter-Strike 2 servers using CounterStrike Sharp*

[English](#english) | [Português](#português)

</div>

---

## English

### 🎯 Overview

The **Deathrun Manager Plugin** is a feature-rich plugin designed specifically for Counter-Strike 2 deathrun servers. It automatically manages team selection, enforces deathrun rules, and provides a smooth gameplay experience for players.

### ✨ Features

- **🎲 Random Terrorist Selection**: Automatically selects a random player as terrorist each round
- **🐰 Bunnyhopping Support**: Configurable bunnyhopping with proper air acceleration settings
- **⚡ Speed Boost**: Configurable velocity multiplier for terrorist players
- **🚫 Command Blocking**: Blocks suicide and exploit commands
- **🗺️ Map Detection**: Smart detection of deathrun maps (dr_* and deathrun_* prefixes)
- **👥 Team Management**: Enforces proper team distribution and prevents unwanted team switches
- **🧹 Weapon Cleanup**: Removes weapons from ground and alive CTs at round end
- **🎨 Colored Messages**: Beautiful colored chat messages for better user experience
- **⚙️ Configurable**: Extensive configuration options via console commands and config file

### 🚀 Installation

1. **Prerequisites**:
   - Counter-Strike 2 server
   - CounterStrike Sharp installed and configured

2. **Download & Install**:
   ```bash
   # Download the plugin files
   git clone https://github.com/yourusername/cs2-deathrun-manager
   
   # Copy to your plugins directory
   cp -r DeathrunManagerPlugin /path/to/counterstrikesharp/plugins/
   ```

3. **Configuration**:
   Create or edit `DeathrunManagerPlugin.json` in your configs folder:
   ```json
   {
     "DrPrefix": "DR Manager",
     "DrEnabled": 1,
     "DrAllowCTGoSpec": 1,
     "DrOnlyDeathrunMaps": 1,
     "DrEnableBunnyhop": 1,
     "DrVelocityMultiplierTR": 1.75
   }
   ```

### ⚙️ Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `DrPrefix` | string | "DR Manager" | Chat prefix for plugin messages |
| `DrEnabled` | int | 1 | Enable/disable the plugin (0/1) |
| `DrAllowCTGoSpec` | int | 1 | Allow CTs to switch to spectator (0/1) |
| `DrOnlyDeathrunMaps` | int | 1 | Only activate on deathrun maps (0/1) |
| `DrEnableBunnyhop` | int | 1 | Enable bunnyhopping on server (0/1) |
| `DrVelocityMultiplierTR` | float | 1.75 | Speed multiplier for terrorist |

### 🎮 Console Commands

| Command | Parameters | Description |
|---------|------------|-------------|
| `dr_enabled` | [1/0] | Enable or disable the plugin |
| `dr_prefix` | [text] | Change the chat prefix |
| `dr_velocity_multiplier_tr` | [number] | Set terrorist speed multiplier |
| `dr_allow_ct_spec` | [1/0] | Allow CT to go spectator |
| `dr_only_deathrun_maps` | [1/0] | Restrict plugin to deathrun maps only |
| `dr_enable_bunnyhop` | [1/0] | Enable or disable bunnyhopping |

### 🎯 How It Works

1. **Map Detection**: Plugin detects if current map is a deathrun map (starts with `dr_` or `deathrun_`)
2. **Round Start**: All players are moved to CT team and given only knives
3. **Terrorist Selection**: One random CT player is selected as terrorist
4. **Speed Boost**: Selected terrorist receives configurable speed boost
5. **Team Enforcement**: Prevents players from switching to terrorist team
6. **Cleanup**: Removes weapons from ground and alive CTs at round end

### 🚫 Blocked Commands

The plugin automatically blocks these exploit commands:
- `kill`
- `killvector`
- `explodevector` 
- `explode`

### 🎨 Features

- **Colored Chat Messages**: Professional looking messages with color coding
- **Automatic Weapon Cleanup**: Removes dropped weapons and strips alive CTs
- **Smart Team Management**: Enforces 1 terrorist vs multiple CTs gameplay
- **Map-Specific Activation**: Only runs on actual deathrun maps when configured

### 🛠️ Development

```bash
# Build the project
dotnet build

# Run tests (if available)
dotnet test
```

### 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

### 🐛 Issues & Support

If you encounter any issues or need support:
- Open an issue on GitHub
- Provide server logs and configuration details
- Describe steps to reproduce the problem

---

## Português

### 🎯 Visão Geral

O **Deathrun Manager Plugin** é um plugin completo projetado especificamente para servidores de deathrun do Counter-Strike 2. Ele gerencia automaticamente a seleção de times, aplica regras de deathrun e proporciona uma experiência de jogo suave para os jogadores.

### ✨ Funcionalidades

- **🎲 Seleção Aleatória de Terrorista**: Seleciona automaticamente um jogador aleatório como terrorista a cada round
- **🐰 Suporte a Bunnyhop**: Bunnyhop configurável com configurações adequadas de aceleração no ar
- **⚡ Aumento de Velocidade**: Multiplicador de velocidade configurável para jogadores terroristas
- **🚫 Bloqueio de Comandos**: Bloqueia comandos de suicídio e exploits
- **🗺️ Detecção de Mapas**: Detecção inteligente de mapas de deathrun (prefixos dr_* e deathrun_*)
- **👥 Gerenciamento de Times**: Força distribuição adequada de times e previne mudanças indesejadas
- **🧹 Limpeza de Armas**: Remove armas do chão e de CTs vivos no final do round
- **🎨 Mensagens Coloridas**: Mensagens coloridas no chat para melhor experiência
- **⚙️ Configurável**: Opções extensivas de configuração via comandos de console e arquivo de config

### 🚀 Instalação

1. **Pré-requisitos**:
   - Servidor Counter-Strike 2
   - CounterStrike Sharp instalado e configurado

2. **Download & Instalação**:
   ```bash
   # Baixar os arquivos do plugin
   git clone https://github.com/yourusername/cs2-deathrun-manager
   
   # Copiar para o diretório de plugins
   cp -r DeathrunManagerPlugin /caminho/para/counterstrikesharp/plugins/
   ```

3. **Configuração**:
   Criar ou editar `DeathrunManagerPlugin.json` na pasta de configs:
   ```json
   {
     "DrPrefix": "DR Manager",
     "DrEnabled": 1,
     "DrAllowCTGoSpec": 1,
     "DrOnlyDeathrunMaps": 1,
     "DrEnableBunnyhop": 1,
     "DrVelocityMultiplierTR": 1.75
   }
   ```

### ⚙️ Opções de Configuração

| Opção | Tipo | Padrão | Descrição |
|-------|------|--------|-----------|
| `DrPrefix` | string | "DR Manager" | Prefixo do chat para mensagens do plugin |
| `DrEnabled` | int | 1 | Habilitar/desabilitar o plugin (0/1) |
| `DrAllowCTGoSpec` | int | 1 | Permitir CTs irem para espectador (0/1) |
| `DrOnlyDeathrunMaps` | int | 1 | Ativar apenas em mapas de deathrun (0/1) |
| `DrEnableBunnyhop` | int | 1 | Habilitar bunnyhop no servidor (0/1) |
| `DrVelocityMultiplierTR` | float | 1.75 | Multiplicador de velocidade para terrorista |

### 🎮 Comandos de Console

| Comando | Parâmetros | Descrição |
|---------|------------|-----------|
| `dr_enabled` | [1/0] | Habilitar ou desabilitar o plugin |
| `dr_prefix` | [texto] | Alterar o prefixo do chat |
| `dr_velocity_multiplier_tr` | [número] | Definir multiplicador de velocidade do terrorista |
| `dr_allow_ct_spec` | [1/0] | Permitir CT ir para espectador |
| `dr_only_deathrun_maps` | [1/0] | Restringir plugin apenas a mapas de deathrun |
| `dr_enable_bunnyhop` | [1/0] | Habilitar ou desabilitar bunnyhop |

### 🎯 Como Funciona

1. **Detecção de Mapa**: Plugin detecta se o mapa atual é de deathrun (começa com `dr_` ou `deathrun_`)
2. **Início do Round**: Todos os jogadores são movidos para o time CT e recebem apenas facas
3. **Seleção de Terrorista**: Um jogador CT aleatório é selecionado como terrorista
4. **Aumento de Velocidade**: Terrorista selecionado recebe aumento de velocidade configurável
5. **Controle de Times**: Previne jogadores de mudarem para o time terrorista
6. **Limpeza**: Remove armas do chão e de CTs vivos no final do round

### 🚫 Comandos Bloqueados

O plugin bloqueia automaticamente estes comandos de exploit:
- `kill`
- `killvector`
- `explodevector`
- `explode`

### 🎨 Características

- **Mensagens Coloridas no Chat**: Mensagens profissionais com codificação de cores
- **Limpeza Automática de Armas**: Remove armas derrubadas e desarma CTs vivos
- **Gerenciamento Inteligente de Times**: Força gameplay de 1 terrorista vs múltiplos CTs
- **Ativação Específica por Mapa**: Roda apenas em mapas de deathrun quando configurado

### 🛠️ Desenvolvimento

```bash
# Compilar o projeto
dotnet build

# Executar testes (se disponível)
dotnet test
```

### 🤝 Contribuindo

1. Faça um fork do repositório
2. Crie uma branch de feature (`git checkout -b feature/funcionalidade-incrivel`)
3. Faça commit das suas mudanças (`git commit -m 'Adicionar funcionalidade incrível'`)
4. Faça push para a branch (`git push origin feature/funcionalidade-incrivel`)
5. Abra um Pull Request

### 📝 Licença

Este projeto está licenciado sob a Licença MIT - veja o arquivo [LICENSE](LICENSE) para detalhes.

### 🐛 Issues & Suporte

Se você encontrar algum problema ou precisar de suporte:
- Abra uma issue no GitHub
- Forneça logs do servidor e detalhes da configuração
- Descreva os passos para reproduzir o problema

---

<div align="center">

**Made with ❤️ for the Counter-Strike community**

*Feito com ❤️ para a comunidade Counter-Strike*

</div>