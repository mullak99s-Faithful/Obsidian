# Obsidian

Web App, Web API, and Discord Bot for managing mullak99's Faithful and FaithfulVenom

## Requirements
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
- [MongoDB](https://www.mongodb.com/try/download/community)
- [Auth0](https://auth0.com/)

## Using the SDK (Obsidian.SDK)
- TODO

## Building the API (Obsidian.API)
1) Open the `Obsidian.API.sln` solution
2) Rename `appsettings.json.example` to `appsettings.json`
3) Edit the `appsettings.json`
4) Press F5, or press the green run button at the top
5) A browser should automatically open to the Swagger UI

## Building the Web App (Obsidian.App)
1) Open the `Obsidian.App.sln` solution
2) Edit the `appsettings.json`
3) Press F5, or press the green run button at the top
4) A browser should automatically open to the Web App

Note: The default `appsettings.json` points to a locally running API. You will get errors if this is not running, or if it is incorrectly configured!

## Building the Discord Bot (Obsidian.Bot)
1) Open the `Obsidian.Bot.sln` solution
2) Rename `appsettings.json.example` to `appsettings.json`
3) Edit the `appsettings.json`
4) Press F5, or press the green run button at the top
5) A terminal window should open

Note: The default `appsettings.json` will need a bot token and the credentials for a Auth0 user (Username-Password-Authentication). Just like the Web App, the it points to a locally running API.
