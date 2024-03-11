# About
- using **.NET Worker Service**
- configured for **Windows**
- running in the background, when pc is turned on
- changing monitor settings (brightness, contrast, etc.) based on time of day
- using **ControlMyMonitor** and **DDC/CI** to change monitor settings
- using MQTT client to receive commands from home automation system

# Install
- add [ControlMyMonitor.exe](https://www.nirsoft.net/utils/controlmymonitor.zip) to PATH
- configure `appsettings.json`
- publish project
- run published `.exe` file with system startup