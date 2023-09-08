# About
- using `.NET Worker Service`
- running in the background, when pc is turned on, even if user isn't logged in
- configured to be created as windows service, can be configured for other platforms (Azure, Linux etc.)   

# Install
- how to build & publish to a folder (or automate it)
  - publish to folder (release, portable)
  - run PowerShell as admin and use service control manager tool:
    - `sc create YourServiceName binPath= "C:\Path\To\YourApp\YourApp.exe" start= auto`
    - `sc start YourServiceName`

# Uninstall
- stop service
- `sc delete YourServiceName` (close Services window if opened)