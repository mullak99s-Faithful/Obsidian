# Obsidian

Web App and API for managing mullak99's Faithful and FaithfulVenom

## Requirements
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
- [MongoDB](https://www.mongodb.com/try/download/community)
- [Auth0](https://auth0.com/)

## Building the API (Obsidian.API)
1) Open the `Obsidian.API.sln` solution
2) Rename `appsettings.json.example` to `appsettings.json`
3) Edit the `appsettings.json`, ensure the MongoDB connection string is correct, and set the Auth0 settings
4) Press F5, or press the green run button at the top
5) A browser should automatically open to the Swagger UI

## Building the Web App (Obsidian.App)
1) Open the `Obsidian.App.sln` solution
2) Press F5, or press the green run button at the top
3) A browser should automatically open to the Web App

Note: The default `appsettings.json` points to a locally running API. You will get errors if this is not running, or if it is incorrectly configured!
