# About
- using `.NET Worker Service`
- running in the background, when pc is turned on, even if user isn't logged in
- configured to be created as windows service, can be configured for other platforms (Azure, Linux etc.)   

# Install
- add ControlMyMonitor.exe to PATH
- how to build & publish to a folder (or automate it)
  - publish to folder with publish profile
  - run PowerShell as admin and use service control manager tool:
    - `sc.exe create "MQTT Client" binPath= "...\MqttClient\MqttClient\bin\Release\net7.0\win-x64\MqttClient.exe" start= auto`
    - `sc.exe start "MQTT Client"`

# Uninstall
- stop service `sc.exe stop "MQTT Client"`
- delete service `sc.exe delete "MQTT Client"` (close Services window if opened)