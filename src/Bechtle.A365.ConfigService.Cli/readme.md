# ConfigService CLI

## Overview

- capabilities
- usage

## Capabilities

The CLI is purpose-built for common tasks that *must* be possible, even if the accompanying (https://shdebonvtfs1.bechtle.net/DefaultCollection/A365/_git/AdminService)[AdminService] is unavailable.

The supported actions are:

1. Export Data from the target ConfigService
2. Import Data to the target ConfigService

## Usage

The CLI provides subcommands for each distinct action it supports.  
You can always view a help-menu by using the `--help` option.  

You always need to specify the ConfigService you're talking to before providing the command, by providing a `-s|--service` option pointing to its root (e.g. `https://a365configurationservice.a365dev.de:8456/`).

```sh
dotnet Bechtle.A365.ConfigService.Cli -s https://a365configurationservice.a365dev.de:8456/ [command] [command-options]
```

### Export

The `export` command allows you to export data (environments only, ATM).

To use this command you have to specify at least one parameter, `--Environment`.  
As per the description, `--Environment` declares an Environment to export, given in `{Category}/{Name}` form (e.g. `av360/dev`).

Using this command like this will write the output to `stdout`, to be piped to a file or another application.  
Alternatively you can specify `--Output` to write directly into a file.

### Import

The `import` command allows you to import data (environments only, ATM) which was previously exported via `export`.

You need to either provide the data via `stdin` or point to a file with `--File`.
