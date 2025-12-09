// InfoPanel.VBZ v1.0.0 - InfoPanel Plugin Template
using InfoPanel.Plugins;
using InfoPanel.VBZ.Services;
using InfoPanel.VBZ.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace InfoPanel.VBZ
{
    /// <summary>
    /// Template plugin for InfoPanel - InfoPanel plugin for VBZ monitoring
    /// 
    /// This template provides a solid foundation for creating new InfoPanel plugins with:
    /// - Service-based architecture
    /// - Event-driven data flow
    /// - Thread-safe sensor updates
    /// - Proper resource management
    /// - Configuration support
    /// 
    /// TODO: Customize this plugin for your specific monitoring needs
    /// </summary>
    public class VBZMain : BasePlugin
    {
        #region Configuration

        // Configuration file path - exposed to InfoPanel for direct file access
        private string? _configFilePath;

        /// <summary>
        /// Exposes the configuration file path to InfoPanel for the "Open Config" button
        /// </summary>
        public override string? ConfigFilePath => _configFilePath;

        #endregion

        #region Sensors

        private readonly PluginText _statusSensor = new("status", "Status", "Initializing...");
        private readonly PluginText _stationSensor = new("station_name", "Station Name", "Loading...");
        private readonly List<PluginText> _lineSensors = new();
        private readonly List<PluginText> _destSensors = new();
        private readonly List<PluginText> _timeSensors = new();

        // Table Sensor
        private static readonly string _departuresTableFormat = "0:30|1:50|2:200|3:50|4:80"; // Icon|Line|Dest|Plat|Time
        private readonly DataTable _departuresDataTable = new();
        private readonly PluginTable _departuresTable;

        #endregion

        #region Services

        private MonitoringService? _monitoringService;
        private ConfigurationService? _configService;
        private FileLoggingService? _loggingService;
        private CancellationTokenSource? _cancellationTokenSource;

        #endregion

        #region Constructor & Initialization

        public VBZMain() : base("InfoPanel.VBZ", "InfoPanel VBZ Monitor", "InfoPanel plugin for VBZ monitoring")
        {
            // Initialize DataTable columns
            _departuresDataTable.Columns.Add("Icon", typeof(PluginText));
            _departuresDataTable.Columns.Add("Line", typeof(PluginText));
            _departuresDataTable.Columns.Add("Destination", typeof(PluginText));
            _departuresDataTable.Columns.Add("Platform", typeof(PluginText));
            _departuresDataTable.Columns.Add("Time", typeof(PluginText));

            _departuresTable = new PluginTable("departures_table", "Departures Table", _departuresDataTable, _departuresTableFormat);

            try
            {
                // Note: _configFilePath will be set in Initialize()
                // ConfigurationService will be initialized after we have the path
            }
            catch (Exception ex)
            {
                // Log initialization errors
                Console.WriteLine($"[VBZ] Error during initialization: {ex.Message}");
                throw;
            }
        }

        public override void Initialize()
        {
            try
            {
                // Set up configuration file path for InfoPanel integration
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string basePath = assembly.ManifestModule.FullyQualifiedName;
                _configFilePath = $"{basePath}.ini";

                Console.WriteLine($"[VBZ] Config file path: {_configFilePath}");

                // Initialize services now that we have the config path
                _configService = new ConfigurationService(_configFilePath);
                _loggingService = new FileLoggingService(_configService);
                _monitoringService = new MonitoringService(_configService, _loggingService);

                // Subscribe to events
                _monitoringService.DataUpdated += OnDataUpdated;

                _loggingService.LogInfo("[VBZ] Plugin initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VBZ] Error during plugin initialization: {ex.Message}");
                _loggingService?.LogError($"[VBZ] Error during plugin initialization: {ex.Message}", ex);
                throw;
            }
        }
        public override void Load(List<IPluginContainer> containers)
        {
            try
            {
                // Create sensor container
                var container = new PluginContainer("VBZ");

                container.Entries.Add(_statusSensor);
                container.Entries.Add(_stationSensor);
                container.Entries.Add(_departuresTable);

                // Create sensors for departures based on config
                int numResults = _configService?.NumberOfResults ?? 5;
                for (int i = 0; i < numResults; i++)
                {
                    var lineSensor = new PluginText($"dep_{i + 1}_line", $"Line {i + 1}", "-");
                    var destSensor = new PluginText($"dep_{i + 1}_dest", $"Destination {i + 1}", "-");
                    var timeSensor = new PluginText($"dep_{i + 1}_time", $"Time {i + 1}", "-");

                    _lineSensors.Add(lineSensor);
                    _destSensors.Add(destSensor);
                    _timeSensors.Add(timeSensor);

                    container.Entries.Add(lineSensor);
                    container.Entries.Add(destSensor);
                    container.Entries.Add(timeSensor);
                }

                // Register container with InfoPanel
                containers.Add(container);

                // Start monitoring
                _cancellationTokenSource = new CancellationTokenSource();
                _ = StartMonitoringAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VBZ] Error during plugin loading: {ex.Message}");
                _loggingService?.LogError($"[VBZ] Error during plugin loading: {ex.Message}", ex);
                throw;
            }
        }

        public override void Close()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _monitoringService?.Dispose();
                _loggingService?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VBZ] Error during disposal: {ex.Message}");
                _loggingService?.LogError($"[VBZ] Error during disposal: {ex.Message}", ex);
            }
        }

        public override TimeSpan UpdateInterval => TimeSpan.FromMilliseconds(_configService?.MonitoringIntervalMs ?? 30000);

        public override void Update()
        {
            // Logic handled by internal timer in MonitoringService
        }

        public override Task UpdateAsync(CancellationToken token)
        {
            // Logic handled by internal timer in MonitoringService
            return Task.CompletedTask;
        }

        #endregion

        #region Monitoring

        private async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_monitoringService != null)
                {
                    await _monitoringService.StartMonitoringAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                Console.WriteLine("[VBZ] Monitoring cancelled");
                _loggingService?.LogInfo("[VBZ] Monitoring cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VBZ] Error in monitoring: {ex.Message}");
                _loggingService?.LogError($"[VBZ] Error in monitoring: {ex.Message}", ex);
            }
        }

        #endregion

        #region Event Handlers

        private void OnDataUpdated(object? sender, DataUpdatedEventArgs e)
        {
            try
            {
                if (e.Data.HasError)
                {
                    _statusSensor.Value = $"Error: {e.Data.ErrorMessage}";
                    return;
                }

                _statusSensor.Value = $"Updated: {e.Data.Timestamp:HH:mm:ss}";
                if (!string.IsNullOrEmpty(e.Data.StationName))
                {
                    _stationSensor.Value = e.Data.StationName;
                }

                // Update table
                lock (_departuresDataTable)
                {
                    _departuresDataTable.Rows.Clear();
                    foreach (var dep in e.Data.Departures)
                    {
                        string icon = GetTransportIcon(dep.TransportMode);
                        
                        // Create colored line text if colors are available
                        var lineText = new PluginText("", "", dep.Line);
                        // Note: PluginText might not support direct color properties in this version,
                        // but we populate the model for future support or if the UI handles it.
                        
                        _departuresDataTable.Rows.Add(
                            new PluginText("", "", icon),
                            lineText,
                            new PluginText("", "", dep.Destination),
                            new PluginText("", "", dep.Platform),
                            new PluginText("", "", dep.FormattedTime)
                        );
                    }
                }

                // Update departure sensors
                for (int i = 0; i < _lineSensors.Count; i++)
                {
                    if (i < e.Data.Departures.Count)
                    {
                        var dep = e.Data.Departures[i];
                        _lineSensors[i].Value = dep.Line;
                        _destSensors[i].Value = dep.Destination;
                        _timeSensors[i].Value = dep.FormattedTime;
                    }
                    else
                    {
                        // Clear unused sensors
                        _lineSensors[i].Value = "-";
                        _destSensors[i].Value = "-";
                        _timeSensors[i].Value = "-";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VBZ] Error updating sensors: {ex.Message}");
                _loggingService?.LogError($"[VBZ] Error updating sensors: {ex.Message}", ex);
                _statusSensor.Value = "Error updating display";
            }
        }

        private string GetTransportIcon(string mode)
        {
            return mode.ToLower() switch
            {
                "tram" => "🚋",
                "bus" => "🚌",
                "rail" => "🚆",
                "funicular" => "🚠",
                "ferry" => "⛴️",
                "gondola" => "🚠",
                _ => "🚍"
            };
        }

        #endregion

    }
}