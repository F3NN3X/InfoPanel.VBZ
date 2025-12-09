# 🚋 InfoPanel.VBZ

![Version](https://img.shields.io/badge/version-1.1.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

**Bring real-time Swiss public transport data directly to your InfoPanel.**

InfoPanel.VBZ is a powerful plugin for the InfoPanel dashboard system that connects to the OpenTransportData Swiss API. Whether you're commuting from Zürich HB or a remote stop in the Alps, get live departure times, delay information, and accessibility status at a glance.

---

## ✨ Features

- **🚍 Real-time Departures**: Monitors public transport departures using the official OJP 2020 API.
- **⏱️ Live Delays**: Displays real-time delay information so you never miss a connection.
- **♿ Accessibility Info**: Instantly see which connections are wheelchair accessible.
- **🌍 Universal Coverage**: Monitor **any** stop in Switzerland (VBZ, SBB, PostBus, etc.).
- **🛡️ Robust & Reliable**: Automatic reconnection, error handling, and detailed logging.

---

## 🚀 Getting Started

### Prerequisites

- **InfoPanel** installed and running.
- **.NET 8.0 Runtime** (or SDK).
- An **OpenTransportData Swiss API Key** (free).

### Installation

1. Copy `InfoPanel.VBZ.dll` (and dependencies) to your InfoPanel `Plugins` folder.
2. Restart InfoPanel.
3. The plugin will generate a default configuration file: `InfoPanel.VBZ.dll.ini`.

---

## ⚙️ Configuration

To get the plugin working, you need to configure your API key and the stop you want to monitor.

### 1. Get your API Key 🔑

You need a free API Key from OpenTransportData Swiss.

1. **Register**: Create an account at [opentransportdata.swiss](https://opentransportdata.swiss/en/dev-dashboard/).
2. **Create App**: Go to the Developer Dashboard and create a new Application.
3. **Subscribe**:
   - Go to the **API Catalogue**.
   - Find **"OJP 1.0 (aka OJP 2020)"**.
   - Click **Subscribe** and select your App.
4. **Copy Key**: Go to your **Applications** page, open your App, and copy the **Consumer Key**.

### 2. Find your Stop ID 📍

You need the unique BAV ID (DiDok number) for your stop.

1. Go to the [Swiss Federal Administration Map](https://map.geo.admin.ch/?topic=ech&lang=en&bgLayer=ch.swisstopo.pixelkarte-farbe&layers=ch.bav.haltestellen-oev).
2. Search for your stop (e.g., "Zürich, Paradeplatz").
3. Click on the stop symbol on the map.
4. Copy the number listed under **"Nummer"** (e.g., `8503000`).

### 3. Edit Configuration 📝

Open `InfoPanel.VBZ.dll.ini` and update the settings:

```ini
[VBZ Settings]
; Your OpenTransportData Swiss API Key
ApiKey=YOUR_LONG_API_KEY_HERE

; The BAV ID of the stop to monitor (e.g., 8503000 for Zürich HB)
StopPointId=8503000

; Number of departures to display
NumberOfResults=5

[Monitoring Settings]
; Update interval in milliseconds (30000 = 30 seconds)
MonitoringIntervalMs=30000
```ini

---

## 🛠️ Troubleshooting

Having trouble? Here are some common fixes.

```

### ❌ No Data Appearing

- Check your **API Key**: Ensure you subscribed to the "OJP 1.0" API in the portal.
- Check your **Stop ID**: Verify the number on [map.geo.admin.ch](https://map.geo.admin.ch).
- Check your **Internet**: The plugin needs to reach `api.opentransportdata.swiss`.

```ini
[Debug Settings]
EnableDebugLogging=true
DebugLogLevel=Debug
```

A file named `VBZ-debug.log` will appear in the plugin directory.

</details>

---

## 🏗️ Building from Source

Want to contribute or modify the code?

```powershell
# Clone the repo
git clone https://github.com/F3NN3X/InfoPanel.VBZ.git

# Build for Release
dotnet build -c Release
```

**Requirements:**

- .NET 8.0 SDK
- Visual Studio 2022 or JetBrains Rider

---

## 📄 License

Distributed under the MIT License. See `LICENSE` for more information.

**Author:** F3NN3X  
**Version:** 1.1.0
