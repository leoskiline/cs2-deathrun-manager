# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned Features
- Advanced statistics tracking
- Multi-language support improvements

---

## [0.5.0] - 2025-08-18

### 🎉 Major Release - Enhanced Deathrun Experience

### Added
- **🛡️ Fall Damage Protection System**
  - Complete fall damage immunity for terrorists
  - Configurable via `dr_terrorist_no_fall_damage` command
  - Real-time damage healing mechanism
  - Automatic health restoration on fall damage detection

- **📝 Comprehensive Logging System**
  - Multiple log levels (INFO, WARN, ERROR, DEBUG)
  - Configurable detailed logging via `DrEnableDetailedLogging`
  - Automatic log file cleanup with retention system
  - Date-organized log files (yyyy-MM-dd format)
  - Robust error handling to prevent log system failures
  - Configurable log retention period (1-365 days)

- **⚙️ Enhanced Configuration Management**
  - New `DrTerroristNoFallDamage` configuration option
  - New `DrEnableDetailedLogging` configuration option  
  - New `DrLogRetentionDays` configuration option
  - Comprehensive input validation for all configuration values
  - Error messages with detailed validation feedback

- **🔄 Hot Reload Support**
  - Configuration changes can be applied without server restart
  - Dynamic plugin state management
  - Improved plugin initialization process

### Enhanced
- **🧹 Advanced Weapon Management System**
  - Improved weapon cleanup with fallback mechanisms
  - Enhanced ground weapon detection and removal
  - Better error handling for weapon operations
  - Optimized performance for weapon cleanup operations

- **👥 Robust Team Management**
  - Enhanced player validation system
  - Improved team switching prevention logic
  - Better handling of spectator team transitions
  - More reliable team state management

- **🎯 Improved Terrorist Selection**
  - Enhanced random selection algorithm
  - Better player validation before selection
  - Improved selection notification system
  - More reliable terrorist assignment process

### Fixed
- Fixed potential null reference exceptions in player validation
- Fixed weapon cleanup edge cases that could cause server lag
- Fixed team switching exploits in certain scenarios
- Improved plugin stability during map changes
- Fixed potential memory leaks in logging system

### Technical Improvements
- Added extensive error handling throughout the codebase
- Implemented proper resource disposal patterns
- Enhanced code organization with better separation of concerns
- Improved performance optimizations
- Added comprehensive input validation

---

## [0.4.0] - 2025-08-18

### Added
- **🎨 Enhanced User Experience**
  - Improved colored chat messages with consistent formatting
  - Better visual feedback for plugin status changes
  - Enhanced notification system for player actions

- **⚡ Performance Optimizations**
  - Optimized player validation checks
  - Improved memory usage patterns
  - Enhanced plugin load times

### Enhanced
- **🗺️ Smart Map Detection**
  - More reliable deathrun map detection
  - Support for custom map prefixes
  - Better error handling for map validation

- **🔧 Server Configuration Management**
  - Automatic application of optimal deathrun server settings
  - Improved bunnyhopping configuration management
  - Better handling of server command execution

### Fixed
- Fixed bunnyhopping settings not applying correctly on some maps
- Improved plugin compatibility with other CounterStrike Sharp plugins
- Fixed rare crashes during plugin initialization

---

## [0.3.0] - 2025-08-17

### Added
- **🐰 Bunnyhopping System**
  - Full bunnyhopping support with proper physics settings
  - Configurable via `dr_enable_bunnyhop` command
  - Automatic server configuration for optimal bunnyhop experience
  - Toggle between bunnyhopping and default movement settings

- **⚡ Terrorist Speed Boost**
  - Configurable velocity multiplier for terrorist players
  - Default 1.75x speed boost for balanced gameplay
  - Real-time speed application on terrorist selection
  - Console command for dynamic speed adjustment

### Enhanced
- **🎮 Console Commands System**
  - Added comprehensive command validation
  - Improved error messages and user feedback
  - Better parameter parsing and validation
  - Enhanced command help system

- **⚙️ Configuration System**
  - Extended configuration options
  - Better default values for optimal gameplay
  - Improved configuration validation

### Fixed
- Fixed team assignment issues on certain maps
- Improved plugin stability during round transitions
- Fixed weapon cleanup not working properly in some scenarios

---

## [0.2.0] - 2025-02-03

### Added
- **👥 Advanced Team Management**
  - Intelligent team switching prevention
  - Support for spectator team management
  - Configurable CT-to-spectator transitions
  - Enhanced team balance enforcement

- **🧹 Weapon Cleanup System**
  - Automatic weapon removal from ground
  - CT weapon stripping at round end
  - Knife-only gameplay enforcement
  - Improved cleanup performance

- **🗺️ Map Detection System**
  - Smart deathrun map detection (dr_* and deathrun_* prefixes)
  - Configurable map-specific plugin activation
  - Better map compatibility checking

### Enhanced
- **🎲 Terrorist Selection System**
  - Improved random selection algorithm
  - Better player validation
  - Enhanced selection notifications
  - More reliable team assignment

- **⚙️ Configuration Management**
  - Added multiple configuration options
  - Improved configuration validation
  - Better default settings

### Fixed
- Fixed plugin not activating on some deathrun maps
- Improved error handling during map changes
- Fixed rare crashes with invalid player data

---

## [0.1.0] - 2025-02-01

### 🎉 Initial Release

### Added
- **🎲 Core Terrorist Selection**
  - Random terrorist selection each round
  - Basic team management
  - Round-based player assignment

- **🚫 Command Blocking System**
  - Blocked suicide commands (kill, killvector, etc.)
  - Exploit prevention system
  - Basic command filtering

- **🎨 Basic User Interface**
  - Colored chat messages
  - Plugin status notifications
  - Basic user feedback system

- **⚙️ Initial Configuration**
  - Basic plugin enable/disable functionality
  - Simple configuration file support
  - Essential console commands

### Core Features
- Counter-Strike 2 compatibility
- CounterStrike Sharp integration
- Basic deathrun gameplay mechanics
- Round-based functionality
- Player team management
- Command listener system

### Technical Foundation
- Plugin architecture setup
- Event handling system
- Basic error handling
- Configuration parsing
- Command processing system

---

## Legend

- 🎉 Major features or releases
- ✨ New features
- 🛡️ Security improvements
- 🐛 Bug fixes
- 🔧 Technical improvements
- 📝 Documentation
- ⚡ Performance improvements
- 👥 User management features
- 🗺️ Map-related features
- 🧹 Cleanup and maintenance features
- 🎲 Game mechanics
- 🚫 Security and prevention features
- ⚙️ Configuration and settings
- 🔄 System improvements

---

## Support

For support, bug reports, or feature requests:
- **GitHub Issues**: [Create an issue](https://github.com/leoskiline/cs2-deathrun-manager/issues)
- **Discussions**: [Join the discussion](https://github.com/leoskiline/cs2-deathrun-manager/discussions)

## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details on how to contribute to this project.

---

*This changelog is maintained by the development team and community contributors.*