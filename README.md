# About
- changes monitor settings (brightness, contrast, blue light) based on time of day
- running in the background, when PC is turned on
- currently configured for **Windows**
- using **.NET Worker Service** (for long running background tasks)
- using **ControlMyMonitor** app and **DDC/CI** to change monitor settings
- using optional MQTT client to receive commands from home automation system

# Install
- add [ControlMyMonitor.exe](https://www.nirsoft.net/utils/controlmymonitor.zip) to PATH
- configure `appsettings.json`/`appsettings.Development.json` using `appsettings.template.json`
- publish project
- run published `.exe` file with system startup