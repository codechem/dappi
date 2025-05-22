# dappi - Dotnet API Pre-Programming Interface

dappi is a powerful tool designed to streamline backend API development by automatically generating controllers with CRUD endpoints for a given entity.

## Getting Started

### Prerequisites

You should have [.NET 9](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)  installed. 

### Installation

1. Install the Dappi CLI.
```sh
  dotnet tool install --global Dappi.Cli --version 0.1.5-preview
```
2. Initialize your project
   ```sh
    dappi init --name <PROJECT-NAME> --path <OUTPUT-DIRECTORY> --use-prerelease
   ```
3. Modify the connection string to point to your database in `appsettings.json`
```json
 ...
  "Dappi":  {  
     "PostgresConnection":"YOUR-CONNECTION-STRING"
  },
  ...
   ```
4. Then you're ready to start your project. Navigate to your project's directory and run
   ```sh
    dappi start
   ```

## Features

- **Automatic Controller Generation:** Controllers are automatically generated for CRUD operations and they offer:
    - Filtering by any field
    - Pagination
    - Sorting

## Installation

1. Create a .NET API Project, add nuget package
nope, not working yet
    ```dotnet add package Codechem.Dappi.Generator --version 1.0.0```

## Usage

### 1. Create a Model

To create a model, define a class and apply the `[CCController]` attribute to it. This will automatically generate a controller for the model.

Example:

```csharp
[CCController]
public class Book
{
    public string Name { get; set; }
    public string Surname { get; set; }
}

After saving the file with any IDE, you run the app, and you should be able to access your CRUD endpoints section under /api/book/ url.
