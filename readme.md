# Config Service 2.0

- Overview & Lingo
- Installation
- Nutzung
- Anbindung von Services
- Technische Details
- Known Issues
- DB-Migrations

## Overview

Der neue ConfigService basiert auf der Idee, dass eine Konfiguration aus Umgebungsvariablen + Struktur besteht.

- Struktur
- Environment / Umgebungsvariablen
- Konfiguration

### Struktur

Eine Struktur im kontext des ConfigServices beschreibt wie Umgebungsvariablen zusammengesetzt werden um eine Funktionierende Konfiguration zu erstellen.
Eine Struktur kann komplett aus festen Werten bestehen, oder aus Referenzen auf die Umgebungsvariablen, oder aus einer Mischung dieser.

Eine Struktur wird dem ConfigService als `JSON` bekannt gemacht und sieht in etwa so aus:

``` json
{
    "name": "Bechtle.A365.AdminService",
    "version": 1,
    "structure": {
        "ClientConfiguration": {
            "authority": "{{NamedEndpoints/IdentityService/Uri}}",
            "client_id": "A365.AdminService.FrontEnd",
            "locale": "de",
            "post_logout_redirect_uri": "{{NamedEndpoints/AdminService/Uri}}",
            "redirect_uri": "{{NamedEndpoints/AdminService/Uri}}",
            "response_type": "id_token token",
            "scope": "openid profile A365.WebApi.Query",
            "service_status_refresh_rate": "10000"
        },
        "Endpoints": "{{Endpoints/*}}",
        "LoggingConfiguration": "{{LoggingConfiguration/ImplementationConfiguration}}"
    },
    "variables": {
        "ServiceName": "Bechtle.A365.AdminService"
    }
}
```

- `name` ist der _**unique**_ key zu dieser Struktur
- `version` ist der _**unique**_ identifier zu dieser `structure`
- `structure` ist der logische aufbau dieser konfiguration
- `variables` sind (optionale) Struktur-Lokale variablen auf die die Umgebungsvariablen zugreifen können

Eine Struktur kann mittels `{{Command: Value; Command2: Value2}}` auf bestimmte Werte oder ganze Regionen der Umgebungsvariablen zugreifen.
Mehr dazu unter [Technische Details](#technische_details)

### Environment

Ein Environment wird unter einer Kategorie -> Name Hierarchie gespeichert und besteht aus einem beliebig großen `JSON-Object`.

### Konfiguration

Eine Konfiguration wird aus einer Struktur und einem Environment erstellt, und ist optional zeitlich begrenzt.

## Installation

Durch die Zertifikat-basierte Authentifizierung ist die installation nicht so Straight-Forward wie bei vielen anderen Services.

Der ConfigService benötigt:

1. Ein gültiges Zertifikat
2. Erreichbar unter HTTPS
3. Host (Kestrel / IIS / Azure) und ggf. andere Reverse Proxies müssen korrekt eingestellt sein

Erklärung zu `3.`:  

### Kestrel

In der `Program.cs` muss `UseKestrel()` wie folgt konfiguriert werden.

```c#
public static IWebHost BuildWebHost(string[] args) =>
    WebHost.CreateDefaultBuilder(args)
           .UseStartup<Startup>()
           .UseKestrel(options =>
           {
               options.Listen(IPAddress.Loopback, 5001, listenOptions =>
               {
                   listenOptions.UseHttps(new HttpsConnectionAdapterOptions
                   {
                       ServerCertificate = /* HTTPS Zertifikat */,
                       ClientCertificateMode = ClientCertificateMode.RequireCertificate,
                       ClientCertificateValidation = CertificateValidator.DisableChannelValidation
                   });
               });
           })
           .Build();
```
`ClientCertificateValidation` muss auf `CertificateValidator.DisableChannelValidation` gestellt werden, um Kestrel daran zu hindern die OS-Zertifikat-Validierung zu nutzen - und stattdessen unsere Custom-Validierung durchzuführen.

### IIS

Im IIS Manager

1. Wähle deine `Site` im `Connections`-Tab aus.
2. Öffne `SSL Settings` in der Feature Ansicht.
3. Aktiviere `Require SSL` und wähle `Require` unter `Client Certificates`.

### Azure and Random custom web proxies

Wenn der ConfigService mit Azure oder anderen Proxies genutzt werden soll, lies die original Doku [hier](https://github.com/blowdart/idunno.Authentication/tree/master/src/idunno.Authentication.Certificate)

## Nutzung

> Die Nutzung durch Swagger ist wesentlich leichter als im ConfigService1.0.

Eine ConfigUI ist in Entwicklung, und ist [hier](http://url-zum-repo.com) in Zukunft verfügbar. (Update folgt)

Bevor ein Environment bearbeitet werden kann muss es erstellt werden, die entsprechenden Endpunkte befinden sich unter `/environments/`.
Vorbereitete Environments können als `JSON` oder als `Key=>Value` map deployed werden.
Änderungen müssen mit `Key=>Value` Angaben gemacht werden.

Strukturen können unter `/structures` hinzugefügt werden.
Services die die [Client-Library](https://shdebonvtfs1.bechtle.net/DefaultCollection/A365/_git/a365.RestClient.ConfigService) laden ihre aktuelle Struktur selbst hoch und bauen sich ihre Konfiguration falls notwendig.
Wenn dies vorbereitet werden soll, kann eine neue Struktur mit `POST /structures` hochgeladen werden.

> - Strukturen können nicht gelöscht oder bearbeitet werden
> - Um Änderungen durchzuführen, muss eine neue Struktur hochgeladen werden
> - Um einen fest eingestellten Wert in einer Strukture zu verändern, muss ein neuer Key mit dem absoluten Pfad im Environment erstellt werden.

### Anbindung von Services

Um einen Service an den neuen ConfigService2.0 anzubinden muss folgende liste befolgt werden:

1. Der Service muss die [Client-Library](https://shdebonvtfs1.bechtle.net/DefaultCollection/A365/_git/a365.RestClient.ConfigService) referenzieren und beim Programm-Start zu seinem `IConfigurationBuilder` hinzufügen

In Asp.Net Core 2.0 und folgend erfolgt dies z.B. so:

```csharp
// Program.cs

public static void Main(string[] args) => BuildWebHost(args).Run();

public static IWebHost BuildWebHost(string[] args)
    => WebHost.CreateDefaultBuilder(args)
              .ConfigureAppConfiguration((context, builder) => ConfigureApp(context, builder, args))
              .UseStartup<Startup>()
              .Build();

private static void ConfigureApp(WebHostBuilderContext builderContext,
                                 IConfigurationBuilder builder,
                                 string[] args)
{
    // clear all current sources - may or may not be useful for you
    builder.Sources.Clear();

    // load the configuration used to find the ConfigService
    var preConfig = new ConfigurationBuilder().AddJsonFile("appsettings.json", true)
                                              .AddEnvironmentVariables()
                                              .AddCommandLine(args)
                                              .Build();

    // this is all we need to reach the ConfigService
    var configSettings = preConfig.GetSection("Configuration")
                                  .Get<ConfigurationSettings>();

    // defining the sources in this order provides us with an easy way to override settings locally
    // useful for developing or testing
    // more sources can be added here if you wish
    builder.AddA365ConfigurationSource(configSettings)
           .AddJsonFile("appsettings.json", true, true)
           .AddEnvironmentVariables()
           .AddCommandLine(args);
}
```

2. In einer der Sources die für die `preConfig` genutzt werden, müssen folgende Informationen vorhanden sein:

``` json
{
    "Configuration": {
        "Endpoint": "http://a365configurationservice.a365dev.de",
        "EnvironmentCategory": "av360",
        "EnvironmentName": "dev",
        // one of the following must be set
        // optional
        "StructureLocation": "./configStructure.json",
        // optional
        "StructureName": "Bechtle.A365.AdminService",
        "StructureVersion": 1
    }
}
```

3. Erstelle eine Struktur-Datei in der die aktuelle Struktur eingetragen wird, siehe [Struktur](#struktur) für das Format
4. `StructureLocation` muss von dem working-directory bei runtime auf eine valide Strukur-Datei zeigen
5. ???
6. Profit!

## Technische Details

### Technischer Overview

Der ConfigService2.0 besteht aus zwei Teilen.

1. Service / HTTP-Rest API
2. Projektion

Der Service nimmt Anfragen entgegen, validiert diese wenn möglich und schickt sie als DomainEvents an den darunterliegenden [EventStore](https://eventstore.org/).

Die Projektion bearbeitet diese DomainEvents und schreibt / löscht / ändert Daten in einem öffentlichen Store - aktuell im MS SQLServer.

### Referenzen / Parsing

Das Parsen der Config-Werte um Referenzen zu finden wird vom `IConfigurationParser` und der Konkreten implementation im `ConfigurationParser` übernommen.

Referenzen sind Key-Globale Instruktionen um einen Wert anzupassen.  
Damit ist folgendes gemeint:

> - Instruktionen in Referenzen können einen Kontext verändern der für diesen Key gilt.
> - Mehrere Referenzen können einen Wert ausmachen
> - Eine Referenz kann mehrere Werte ausmachen

Eine Referenz wird mit `{{` geöffnet und mit `}}` geschlossen.  
Innerhalb einer Referenz befinden sich 1..n Instruktionen.  
Eine Instruktion besteht aus einem Kommando und einem Wert: `Path: some/path/to/narnia`

Mögliche Kommandos sind:

- `Path`
- `Using`
- `Alias`

#### Path

Diese Referenz wird durch den / die Keys ausgetauscht die der Value angibt.  
Sollte Value mit `/*` enden wird dieser Value durch alle Keys ausgetauscht die Value matchen, und keine weiteren Kommandos werden verarbeitet.  
> Wenn kein Kommando angegeben wird, wird `Path` impliziert

#### Using &  Alias

Diese Kommandos können nur zusammen genutzt werden und haben alleine keine Auswirkungen

Beispiel:  
Folgende Referenz:  
`{{ Using: some/path/to/narnia; Alias: narnia; }}`

Erstellt einen Eintrag:  
`$narnia => 'some/path/to/narnia'`

Dieser Eintrag kann dann innerhalb des selben Kontexts (Selber Value) mit Angabe des Alias' genutzt werden:  
`{{$narnia/forest/castle/dungeon/cell}}`

Vollständiger Wert:  
`{{ Using: some/path/to/narnia; Alias: narnia; }}{{ $narnia/forest/castle/dungeon/cell }}`

Fertiger Wert:  
`some/path/to/narnia/forest/castle/dungeon/cell`

### Feste Aliases

Beim Parsen einer Referenz stellt der ConfigService2.0 bestimmte Aliases zur Verfügung, ohne dass diese vorher definiert werden müssen.

- `$this`
- `$struct`

#### This

`$this` wird automatisch durch den Pfad des direkten Parents ersetzt.

```json
{
    "some/path/to/narnia": "-",
    "some/path/to/somewhere": "{{$this/neverland}}"
}
```

#### Struct

`$struct` ist insofern speziell, als dass es Zugriff auf das `variables` objekt innerhalb einer Struktur erlaubt.

`{{$struct/MyVariable}}` greift auf `MyVariable` innerhalb der aktuellen Struktur zu.

## Known Issues

### Bestimmte Zeichen in JSON-Properties führen zu falschen Konfigurationen

JSON-Properties die `'/'` enthalten können zu Problemen führen, wenn sie nicht korrekt an den ConfigService übergeben werden.

``` json
{
  "TargetConfigurations": {
    "Ein-/Ausgang (Papier)": "Original",
    "Elektronisches Dokument": "Original",
    "TempDokument": "Original"
  }
}
```

Dieses JSON, als `Dictionary<string, string>` dargestellt, kann unterschiedliche Ergebnisse haben.

Fehlerhafte Darstellung:
> ``` json
> {
>   "TargetConfigurations/Ein-/Ausgang (Papier)": "Original",
>   "TargetConfigurations/Elektronisches Dokument": "Original",
>   "TargetConfigurations/TempDokument": "Original"
> }
> ```

Korrekte Darstellung:
> ```json
> {
>   "TargetConfigurations/Ein-%2FAusgang%20%28Papier%29": "Original",
>   "TargetConfigurations/Elektronisches%20Dokument": "Original",
>   "TargetConfigurations/TempDokument": "Original"
> }
> ```

Alle Pfade sind technisch korrekt, die "Fehlerhafte Darstellung" führt allerdings zu einem anderen JSON als gewollt.

``` json
{
  "TargetConfigurations": {
    "Ein-": {
        "Ausgang (Papier)": "Original"
    },
    "Elektronisches Dokument": "Original",
    "TempDokument": "Original"
  }
}
```

Der ConfigService2.0 wandelt JSON automatisch in das korrekte Format wenn der Endpunkt `/environments/{category}/{name}/json` genutzt wird.
Alternativ steht noch der Endpunkt `/environments/{category}/{name}/keys` zur Verfügung - dort wird allerdings davon ausgegangen dass die übertragenen Werte korrekt sind.  
Über die Endpunkte `/convert/json/map` und `/convert/map/json` können JSON und `Dictionary<string, string>` beliebig umgewandelt werden, falls der Client diese Aufgabe nicht übernehmen will.

## DB-Migrations

Änderungen an dem DB-Schema werden über Migrations versioniert und durchgeführt.
Das Ausführen von Migrations ist näher in der ConfigService.Cli-Readme dokumentiert.

Um mit Migrations zu arbeiten, müssen folgende Schritte befolgt werden:

- Ins Verzeichnis Bechtle.A365.ConfigSerivce/src/Bechtle.A365.ConfigService.Cli springen
- Konsole öffnen
- dotnet 'ef' Befehle mit diesen Parametern ausführen '--context ProjectionStoreContext --project ../Bechtle.A365.ConfigService.Migrations'

z.B. so, um alle Migrations anzuzeigen: 

```
dotnet ef migrations list --context ProjectionStoreContext --project ../Bechtle.A365.ConfigService.Migrations
```
