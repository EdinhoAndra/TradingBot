codex/add-sdk-8.0.411-requirements-section
# Meu Projeto

## .NET SDK

The project targets **.NET SDK 8.0.411** as defined in
[`global.json`](./global.json). If you do not have this version installed, you
can install it using the official `dotnet-install.sh` script:

```bash
curl -SL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --version 8.0.411
```

For more details about the installation script see the
[Microsoft documentation](https://learn.microsoft.com/dotnet/core/tools/dotnet-install-script).

# Trading Bot

This repository contains the source code for a trading bot and its supporting libraries.


## Restoring dependencies

Run the following command from the repository root to download all NuGet packages:

```bash
dotnet restore
```

## Building the solution

Compile all projects in the solution using:

```bash
dotnet build
```

## Running tests

Execute the unit tests with:

```bash
dotnet test
```


### Loading historical data in Renko mode

When using the interactive console, the `start renko` command now prompts for an optional start date (`dd/MM/yyyy HH:mm:ss`).
If provided, historical trades from that moment until now are loaded before subscribing to real-time data.
