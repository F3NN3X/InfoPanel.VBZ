using InfoPanel.VBZ.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InfoPanel.VBZ.Services
{
    /// <summary>
    /// Event arguments for data update events
    /// </summary>
    public class DataUpdatedEventArgs : EventArgs
    {
        public VbzData Data { get; }

        public DataUpdatedEventArgs(VbzData data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Core monitoring service for VBZ data
    /// </summary>
    public class MonitoringService : IDisposable
    {
        #region Events

        /// <summary>
        /// Triggered when new data is available
        /// </summary>
        public event EventHandler<DataUpdatedEventArgs>? DataUpdated;

        #endregion

        #region Fields

        private readonly ConfigurationService _configService;
        private readonly FileLoggingService? _loggingService;
        private readonly System.Threading.Timer _monitoringTimer;
        private volatile bool _isMonitoring;
        private readonly object _lockObject = new();
        private readonly HttpClient _httpClient;

        // API Constants
        private const string ApiUrl = "https://api.opentransportdata.swiss/ojp2020";

        #endregion

        #region Constructor

        public MonitoringService(ConfigurationService configService, FileLoggingService? loggingService = null)
        {
            _configService = configService ?? throw new ArgumentNullException(nameof(configService));
            _loggingService = loggingService;

            // Initialize timer (but don't start it yet)
            _monitoringTimer = new System.Threading.Timer(OnTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

            _httpClient = new HttpClient();
            _loggingService?.LogInfo("[MonitoringService] Service initialized");
        }
        #endregion

        #region Monitoring Control

        /// <summary>
        /// Starts the monitoring process
        /// </summary>
        public async Task StartMonitoringAsync(CancellationToken cancellationToken)
        {
            lock (_lockObject)
            {
                if (_isMonitoring)
                {
                    _loggingService?.LogInfo("[MonitoringService] Already monitoring");
                    return;
                }

                _isMonitoring = true;
            }

            try
            {
                // TODO: Add any startup logic here
                // Examples:
                // await ConnectToDataSourceAsync();
                // await ValidateConfigurationAsync();
                // await InitializeCountersAsync();

                // Start the monitoring timer
                var interval = _configService.MonitoringIntervalMs; // Default: 30000ms
                _monitoringTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(interval));

                _loggingService?.LogInfo($"[MonitoringService] Monitoring started (interval: {interval}ms)");

                // Keep the task alive while monitoring
                while (_isMonitoring && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _loggingService?.LogInfo("[MonitoringService] Monitoring cancelled");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"[MonitoringService] Error during monitoring: {ex.Message}", ex);
                throw;
            }
            finally
            {
                await StopMonitoringAsync();
            }
        }

        /// <summary>
        /// Stops the monitoring process
        /// </summary>
        public async Task StopMonitoringAsync()
        {
            lock (_lockObject)
            {
                if (!_isMonitoring)
                {
                    return;
                }

                _isMonitoring = false;
            }

            // Stop the timer
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // TODO: Add any cleanup logic here
            // Examples:
            // await DisconnectFromDataSourceAsync();
            // await FlushPendingDataAsync();
            // await SaveStateAsync();

            _loggingService?.LogInfo("[MonitoringService] Monitoring stopped");
        }

        #endregion

        #region Data Collection

        private async void OnTimerElapsed(object? state)
        {
            if (!_isMonitoring)
                return;

            try
            {
                // Collect data from your source
                var data = await CollectDataAsync();

                // Notify subscribers
                OnDataUpdated(data);
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"[MonitoringService] Error collecting data: {ex.Message}", ex);

                // Create error data
                var errorData = new VbzData
                {
                    ErrorMessage = $"Collection error: {ex.Message}",
                    Timestamp = DateTime.Now,
                    HasError = true
                };

                OnDataUpdated(errorData);
            }
        }

        /// <summary>
        /// Collects data from VBZ API
        /// </summary>
        private async Task<VbzData> CollectDataAsync()
        {
            var apiKey = _configService.ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return new VbzData
                {
                    HasError = true,
                    ErrorMessage = "API Key not configured"
                };
            }

            if (apiKey.Length < 20)
            {
                _loggingService?.LogWarning($"[MonitoringService] API Key seems very short ({apiKey.Length} chars). Please check if this is a valid token.");
            }

            var stopPointId = _configService.StopPointId;
            var numberOfResults = _configService.NumberOfResults;
            var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss");

            var xmlPayload = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<OJP xmlns=""http://www.vdv.de/ojp"" xmlns:siri=""http://www.siri.org.uk/siri"" version=""1.0"">
    <OJPRequest>
        <siri:ServiceRequest>
            <siri:RequestTimestamp>{now}Z</siri:RequestTimestamp>
            <siri:RequestorRef>InfoPanel.VBZ</siri:RequestorRef>
            <OJPStopEventRequest>
                <siri:RequestTimestamp>{now}Z</siri:RequestTimestamp>
                <Location>
                    <PlaceRef>
                        <siri:StopPointRef>{stopPointId}</siri:StopPointRef>
                        <LocationName>
                            <Text>Stop</Text>
                        </LocationName>
                    </PlaceRef>
                    <DepArrTime>{now}Z</DepArrTime>
                </Location>
                <Params>
                    <NumberOfResults>{numberOfResults}</NumberOfResults>
                    <StopEventType>departure</StopEventType>
                    <IncludePreviousCalls>false</IncludePreviousCalls>
                    <IncludeOnwardCalls>false</IncludeOnwardCalls>
                    <IncludeRealtimeData>true</IncludeRealtimeData>
                </Params>
            </OJPStopEventRequest>
        </siri:ServiceRequest>
    </OJPRequest>
</OJP>";

            var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);

            // Ensure Bearer prefix
            var authHeader = apiKey.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? apiKey : $"Bearer {apiKey}";
            request.Headers.Add("Authorization", authHeader);

            // Add User-Agent header (required by OpenTransportData Swiss)
            request.Headers.Add("User-Agent", "InfoPanel.VBZ/1.0");

            request.Content = new StringContent(xmlPayload, Encoding.UTF8, "application/xml");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _loggingService?.LogError($"[MonitoringService] API Request Failed. Status: {response.StatusCode}. Response: {errorContent}");

                return new VbzData
                {
                    HasError = true,
                    ErrorMessage = $"API Error: {response.StatusCode}"
                };
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            return ParseResponse(responseContent);
        }

        private VbzData ParseResponse(string xmlContent)
        {
            var vbzData = new VbzData();
            try
            {
                var doc = XDocument.Parse(xmlContent);
                XNamespace ojp = "http://www.vdv.de/ojp";
                XNamespace siri = "http://www.siri.org.uk/siri";

                var stopEvents = doc.Descendants(ojp + "StopEventResult");

                foreach (var stopEvent in stopEvents)
                {
                    var departure = new Departure();
                    var stopEventElement = stopEvent.Descendants(ojp + "StopEvent").FirstOrDefault();

                    if (stopEventElement == null) continue;

                    var service = stopEventElement.Descendants(ojp + "Service").FirstOrDefault();
                    if (service != null)
                    {
                        departure.Line = service.Descendants(ojp + "PublishedLineName").FirstOrDefault()?.Descendants(ojp + "Text").FirstOrDefault()?.Value ?? "?";
                        departure.Destination = service.Descendants(ojp + "DestinationText").FirstOrDefault()?.Descendants(ojp + "Text").FirstOrDefault()?.Value ?? "?";

                        // Parse Transport Mode
                        var mode = service.Descendants(ojp + "Mode").FirstOrDefault();
                        if (mode != null)
                        {
                            departure.TransportMode = mode.Descendants(ojp + "PtMode").FirstOrDefault()?.Value ?? "";
                        }

                        // Check for accessibility (Attribute/Code = A__NF)
                        var attrs = service.Descendants(ojp + "Attribute");
                        foreach (var attr in attrs)
                        {
                            if (attr.Descendants(ojp + "Code").FirstOrDefault()?.Value == "A__NF")
                            {
                                departure.IsAccessible = true;
                                break;
                            }
                        }
                    }

                    var thisCall = stopEventElement.Descendants(ojp + "ThisCall").FirstOrDefault();
                    if (thisCall != null)
                    {
                        var callAtStop = thisCall.Descendants(ojp + "CallAtStop").FirstOrDefault();
                        if (callAtStop != null)
                        {
                            // Parse Station Name (only need to do this once per response really, but safe here)
                            if (string.IsNullOrEmpty(vbzData.StationName))
                            {
                                vbzData.StationName = callAtStop.Descendants(ojp + "StopPointName").FirstOrDefault()?.Descendants(ojp + "Text").FirstOrDefault()?.Value ?? "";
                            }

                            // Parse Platform
                            var plannedQuay = callAtStop.Descendants(ojp + "PlannedQuay").FirstOrDefault()?.Descendants(ojp + "Text").FirstOrDefault()?.Value;
                            var estimatedQuay = callAtStop.Descendants(ojp + "EstimatedQuay").FirstOrDefault()?.Descendants(ojp + "Text").FirstOrDefault()?.Value;
                            departure.Platform = !string.IsNullOrEmpty(estimatedQuay) ? estimatedQuay : (plannedQuay ?? "");

                            var serviceDeparture = callAtStop.Descendants(ojp + "ServiceDeparture").FirstOrDefault();
                            if (serviceDeparture != null)
                            {
                                var timetabledTimeStr = serviceDeparture.Descendants(ojp + "TimetabledTime").FirstOrDefault()?.Value;
                                var estimatedTimeStr = serviceDeparture.Descendants(ojp + "EstimatedTime").FirstOrDefault()?.Value;

                                if (DateTime.TryParse(timetabledTimeStr, out var timetabledTime))
                                {
                                    departure.DepartureTime = timetabledTime;
                                }

                                if (!string.IsNullOrEmpty(estimatedTimeStr) && DateTime.TryParse(estimatedTimeStr, out var estimatedTime))
                                {
                                    departure.IsRealtime = true;
                                    departure.DepartureTime = estimatedTime; // Use estimated time for display

                                    // Check if late (e.g. > 3 mins difference)
                                    if ((estimatedTime - timetabledTime).TotalMinutes >= 3)
                                    {
                                        departure.IsLate = true;
                                    }
                                }
                            }
                        }
                    }

                    // Assign Line Colors (VBZ specific fallback)
                    var colors = GetVbzLineColors(departure.Line);
                    departure.LineBackgroundColor = colors.Bg;
                    departure.LineTextColor = colors.Fg;

                    vbzData.Departures.Add(departure);
                }
            }
            catch (Exception ex)
            {
                vbzData.HasError = true;
                vbzData.ErrorMessage = $"Parse error: {ex.Message}";
                _loggingService?.LogError($"[MonitoringService] XML Parse Error: {ex.Message}", ex);
                _loggingService?.LogVerbose($"[MonitoringService] Failed XML: {xmlContent}");
            }

            return vbzData;
        }

        private (string Bg, string Fg) GetVbzLineColors(string line)
        {
            return line switch
            {
                // Trams
                "2" => ("#E30613", "#FFFFFF"), // Red
                "3" => ("#007A33", "#FFFFFF"), // Green
                "4" => ("#3F2985", "#FFFFFF"), // Purple
                "5" => ("#7F5629", "#FFFFFF"), // Brown
                "6" => ("#E88D23", "#FFFFFF"), // Orange
                "7" => ("#1D1D1B", "#FFFFFF"), // Black
                "8" => ("#8BC63E", "#FFFFFF"), // Light Green
                "9" => ("#3F2985", "#FFFFFF"), // Purple
                "10" => ("#DC005D", "#FFFFFF"), // Pink
                "11" => ("#007A33", "#FFFFFF"), // Green
                "12" => ("#009EE0", "#FFFFFF"), // Light Blue
                "13" => ("#F6C90E", "#000000"), // Yellow
                "14" => ("#009EE0", "#FFFFFF"), // Light Blue
                "15" => ("#E30613", "#FFFFFF"), // Red
                "17" => ("#A3238E", "#FFFFFF"), // Violet

                // Trolleybuses
                "31" => ("#009EE0", "#FFFFFF"),
                "32" => ("#1D1D1B", "#FFFFFF"),
                "33" => ("#E88D23", "#FFFFFF"),
                "34" => ("#8BC63E", "#FFFFFF"),
                "46" => ("#E30613", "#FFFFFF"),
                "72" => ("#DC005D", "#FFFFFF"),

                // Default
                _ => ("#FFFFFF", "#000000")
            };
        }

        /// <summary>
        /// Triggers the DataUpdated event
        /// </summary>
        private void OnDataUpdated(VbzData data)
        {
            try
            {
                DataUpdated?.Invoke(this, new DataUpdatedEventArgs(data));
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"[MonitoringService] Error in DataUpdated event: {ex.Message}", ex);
            }
        }

        #endregion

        #region Disposal

        public void Dispose()
        {
            try
            {
                _isMonitoring = false;
                _monitoringTimer?.Dispose();
                _httpClient?.Dispose();

                // TODO: Dispose any resources you created
                // Examples:
                // _cpuCounter?.Dispose();
                // _httpClient?.Dispose();
                // _dbConnection?.Dispose();

                _loggingService?.LogInfo("[MonitoringService] Service disposed");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError($"[MonitoringService] Error during disposal: {ex.Message}", ex);
            }
        }

        #endregion
    }
}