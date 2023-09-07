# About
- using `.NET Worker Service`
- running in the background, when pc is turned on, even if user isn't logged in
- configured to be created as windows service, can be configured for other platforms (Azure, Linux etc.)   

# Install
- how to build & publish to a folder (or automate it)
  - `sc create YourServiceName binPath= "C:\Path\To\YourApp\YourApp.exe"`
  - `sc start YourServiceName`