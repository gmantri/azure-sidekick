# Azure Sidekick

Azure Sidekick is an AI assistant that can answer questions about the resources in your Azure subscriptions. 
You can also use this to ask questions about Azure as well.

## Supported Azure Services

Currently following Azure services are supported:
- Azure Storage
  - General Azure storage questions
  - Questions about storage accounts in your Azure Subscription e.g. How many storage accounts do not have tags?
  - Question about a specific storage account in your Azure Subscription e.g. Is "devstoreaccount1" storage account located in Europe?

## Before you begin

### Sign in into your Azure account
Before you can use this, please make sure that you are signed-in into your Azure account. You can use Azure CLI,
Azure PowerShell, Visual Studio or Visual Studio Code to sign-in into your Azure account. This makes use of your
credentials from there.

### Configure application
Look for `appsettings.template.json` file either in the `src\ConsoleUI` directory (if you are running this in an IDE)
or in the directory where you downloaded and unzipped the binaries (if you are running the executable directly).

Rename that file to `appsettings.json` and provide the values for your Azure OpenAI deployments.

```json
{
  "AzureOpenAISettings": {
    "Endpoint": "Azure OpenAI service endpoint e.g. https://xyz.openai.azure.com/>",
    "Key": "Azure OpenAI service key e.g. xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
    "DeploymentId": "Azure OpenAI model deployment id e.g. gpt-4-32k"
  }
}
```

If you want to use Azure RBAC, do not specify any value for the `Key` parameter. The application will make use of your logged-in credentials
to connect to Azure OpenAI service using RBAC.

### Logging
By default the application logs every operation and exception and saves them to local file system.
The log files are stored in the `Temp` directory on the local computer.

The application also supports Azure Application Insights for logging.
There are some changes need to be made if you want to use that.

1. Add the following section to `appinsights.json`:

```json
  "ApplicationInsights": {
    "LogLevel": {
      "Default": "None"
    },
    "EnableAdaptiveSampling": false,
    "EnableDependencyTrackingTelemetryModule": false,
    "ConnectionString": "<your-application-insights-connection-string>"
  }
```

2. Remove/comment file system logger in `Program.cs`. Look for the following line of code there and remove/comment it:

```csharp
serviceCollection.AddSingleton<ILogger, FileSystemLogger>();
```

3. Add application insights dependency instead:

```csharp
serviceCollection.AddApplicationInsightsTelemetryWorkerService(configuration);
serviceCollection.AddSingleton<ILogger, AppInsightsLogger>();
```
