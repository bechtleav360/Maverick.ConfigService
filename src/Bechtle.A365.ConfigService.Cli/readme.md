# ConfigService CLI

## Overview

- capabilities
- usage

## Capabilities

The CLI is purpose-built for common tasks that *must* be possible, even if the accompanying (https://shdebonvtfs1.bechtle.net/DefaultCollection/A365/_git/AdminService)[AdminService] is unavailable.

The supported actions are:

1. Export Data from the target ConfigService
2. Import Data to the target ConfigService
3. Test the Connection of the target ConfigService

## Usage

The CLI provides subcommands for each distinct action it supports.  
You can always view a help-menu by using the `--help` option.  

You always need to specify the ConfigService you're talking to after providing the command, by providing a `-s|--service` option pointing to its root (e.g. `https://a365configurationservice.a365dev.de:8456/`).

```sh
dotnet Bechtle.A365.ConfigService.Cli [command] -s https://a365configurationservice.a365dev.de:8456/ [command-options]
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

### Test

The `test` command allows you to test a given ConfigService and its Configuration.  
The `test` command will try to emulate the way the ConfigService configures itself and open Connections to the relevant Systems it needs.

To do this you need to provide either the `--config-by-convention` paramter pointing to the ConfigService root-folder, or use the `-c|--config` parameters to declare how the CLI should build the Configuration - more about how to use them can be seen using the `--help` command.

> You will likely also need to provide the `-v` flag to indicate verbose logging.
> You can also increase the amount of information by providing multiple `-v` flags (`-vvv`)

#### Using the `--config` flag

To declare how the CLI should build its Configuration, you have access to three building blocks:

- Environment
- Json Files
- Command-Line Arguments

you can declare them each multiple times with different options to overload the Configuration in different ways.

> Spaces are generally not allowed in the option-values, unless explicitly declared (json-files)

##### Environment

Pattern: `env[ironment][:{PREFIX}]`  
Examples:
- `env`
- `environment`
- `env:ASPNETCORE_`
- `environment:ASPNETCORE_`

the Environment Source can be either `env` or `environment` with an optional prefix

##### Json Files

Pattern: `file:([{path}]|["{path-with-spaces}"])[;req(uired)]`  
Examples:
- `file:appsettings.json`
- `file:appsettings.json;req`
- `file:appsettings.json;required`
- `file:../../appsettings.json`
- `file:../../appsettings.json;req`
- `file:../../appsettings.json;required`
- `file:C:\A365\Service\appsettings.json`
- `file:C:\A365\Service\appsettings.json;req`
- `file:C:\A365\Service\appsettings.json;required`
- `file:"C:\A365\Service\appsettings.json"`
- `file:"C:\A365\Service\appsettings.json";req`
- `file:"C:\A365\Service\appsettings.json";required`
- `file:"C:\A365\Service with spaces\appsettings.json"`
- `file:"C:\A365\Service with spaces\appsettings.json";req`
- `file:"C:\A365\Service with spaces\appsettings.json";required`

the Json-File Source must be a valid file-path, starting and ending with Quotes if spaces are included, and ending with `;req` if its supposed to be required

##### Command-Line Arguments

Pattern: `cli-args`  
Examples:
- `cli-args`

this enables argument pass-thru from this application to the Emulated Configuration.

> this will pass thru all arguments that were passed to this Application after `--`
> e.g.
> Bechtle.A365.ConfigService.exe test -s ... -- --ConfigKey1 ConfigValue1 --ConfigKey2 ...


> for more information see [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2#command-line-configuration-provider) or more specifically [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2#arguments)