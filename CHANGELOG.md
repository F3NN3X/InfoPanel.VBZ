# Changelog

All notable changes to this project will be documented in this file.

## [1.0.0] - 2025-12-09

### Added

- Initial release of InfoPanel.VBZ plugin
- Integration with OpenTransportData Swiss OJP 2020 API (XML interface)
- Real-time departure monitoring with delay calculation
- Accessibility information (A__NF code support)
- Configurable StopPointId (BAV ID) and result count
- File-based debug logging system (`VBZ-debug.log`)
- Comprehensive error handling for API requests and XML parsing
- User-Agent header compliance for OpenTransportData API

### Changed

- Refactored template architecture to focus on VBZ monitoring
- Replaced JSON parsing with XML parsing for OJP compliance
- Updated project structure to remove unused services

## [1.0.1] - 2025-12-09

### Fixed

- Fixed build pipeline issues (PluginInfo.ini missing, project reference path)
- Updated project metadata and repository URLs
